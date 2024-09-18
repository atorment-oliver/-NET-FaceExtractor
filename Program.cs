using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Accord.IO;
using Accord.Statistics.Kernels;
using DlibDotNet;
using DlibDotNet.Extensions;
using Emgu.CV.Reg;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using TestApplication;
using Point = System.Drawing.Point;

class Program
{
    static void Main(string[] args)
    {
        string imagePath = "D://YO.jpeg";
        string sexo = DetermineGender(imagePath);
       ExtractCompleteFace(imagePath);
    }
    static bool IsHeadBald(string headImagePath, string hairImagePath)
    {
        // Cargar las imágenes de la cabeza y el cabello
        Mat headImage = Cv2.ImRead(headImagePath, ImreadModes.Unchanged);
        Mat hairImage = Cv2.ImRead(hairImagePath, ImreadModes.Unchanged);

        // Convertir las imágenes a escala de grises
        Mat grayHead = new Mat();
        Mat grayHair = new Mat();
        Cv2.CvtColor(headImage, grayHead, ColorConversionCodes.BGRA2GRAY);
        Cv2.CvtColor(hairImage, grayHair, ColorConversionCodes.BGRA2GRAY);

        // Aplicar un umbral para crear imágenes binarias
        Mat binaryHead = new Mat();
        Mat binaryHair = new Mat();
        Cv2.Threshold(grayHead, binaryHead, 50, 255, ThresholdTypes.Binary);
        Cv2.Threshold(grayHair, binaryHair, 50, 255, ThresholdTypes.Binary);

        // Contar los píxeles no negros en las imágenes binarias
        int headNonZero = Cv2.CountNonZero(binaryHead);
        int hairNonZero = Cv2.CountNonZero(binaryHair);

        // Calcular la densidad del cabello
        double hairDensity = (double)hairNonZero / headNonZero;

        // Determinar si la cabeza es calva basado en la densidad del cabello
        return hairDensity < 0.01; // Ajustar el umbral según sea necesario
    }
    static bool IsHeadBald(string headImagePath)
    {
        // Cargar la imagen de la cabeza
        Mat headImage = Cv2.ImRead(headImagePath, ImreadModes.Color);

        // Convertir a espacio de color gris
        Mat grayImage = new Mat();
        Cv2.CvtColor(headImage, grayImage, ColorConversionCodes.BGR2GRAY);

        // Aplicar un umbral para crear una imagen binaria
        Mat binaryImage = new Mat();
        Cv2.Threshold(grayImage, binaryImage, 50, 255, ThresholdTypes.Binary);

        // Encontrar contornos en la imagen binaria
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(binaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // Filtrar contornos basados en el área para encontrar el contorno de la cabeza
        OpenCvSharp.Point[] headContour = null;
        double maxArea = 0;
        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > maxArea)
            {
                maxArea = area;
                headContour = contour;
            }
        }

        if (headContour == null)
        {
            return false;
        }

        // Dividir el contorno de la cabeza en la mitad superior y la mitad inferior
        List<OpenCvSharp.Point> topHalf = new List<OpenCvSharp.Point>();
        List<OpenCvSharp.Point> bottomHalf = new List<OpenCvSharp.Point>();
        foreach (var point in headContour)
        {
            if (point.Y < headImage.Rows / 2)
            {
                topHalf.Add(point);
            }
            else
            {
                bottomHalf.Add(point);
            }
        }

        // Calcular el área de la parte superior e inferior
        double topArea = Cv2.ContourArea(topHalf);
        double bottomArea = Cv2.ContourArea(bottomHalf);

        // Verificar si la parte superior tiene una forma semicircular
        double radius = Math.Sqrt(topArea / Math.PI);
        double expectedArcLength = Math.PI * radius; // Longitud esperada del arco semicircular
        double actualArcLength = Cv2.ArcLength(topHalf, true);

        // Comparar la longitud del arco con la longitud esperada del arco semicircular
        return Math.Abs(actualArcLength - expectedArcLength) < expectedArcLength * 0.1; // Ajustar el umbral según sea necesario
    }
    static void ExtractCompleteFace(string imagePath)
    {
        // Cargar la imagen
        Mat image = Cv2.ImRead(imagePath);
        Mat originalImage = image.Clone();

        // Convertir a espacio de color YCrCb
        Mat ycrcb = new Mat();
        Cv2.CvtColor(image, ycrcb, ColorConversionCodes.BGR2YCrCb);

        // Definir rango de color de la piel en YCrCb
        Scalar lower = new Scalar(0, 133, 77);
        Scalar upper = new Scalar(255, 173, 127);

        // Crear una máscara basada en el rango de color de la piel
        Mat mask = new Mat();
        Cv2.InRange(ycrcb, lower, upper, mask);

        // Encontrar contornos en la máscara
        OpenCvSharp.Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // Filtrar contornos basados en el área para encontrar el contorno de la cabeza
        OpenCvSharp.Point[] headContour = null;
        double maxArea = 0;
        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area > maxArea)
            {
                maxArea = area;
                headContour = contour;
            }
        }

        // Crear una máscara para el contorno de la cabeza
        Mat headMask = Mat.Zeros(mask.Size(), MatType.CV_8UC1);
        Cv2.DrawContours(headMask, new OpenCvSharp.Point[][] { headContour }, -1, Scalar.White, thickness: Cv2.FILLED);

        // Aplicar la máscara a la imagen original para extraer la cabeza
        Mat headExtracted = new Mat();
        originalImage.CopyTo(headExtracted, headMask);

        // Crear un fondo transparente para la parte de la imagen que no es la cabeza
        Mat[] channels = Cv2.Split(headExtracted);
        Mat alpha = new Mat(channels[0].Size(), MatType.CV_8UC1, Scalar.All(255));
        Mat headExtractedWithAlpha = new Mat();
        Cv2.Merge(new Mat[] { channels[0], channels[1], channels[2], alpha }, headExtractedWithAlpha);

        // Configurar los píxeles fuera del contorno de la cabeza como transparentes
        for (int i = 0; i < headMask.Rows; i++)
        {
            for (int j = 0; j < headMask.Cols; j++)
            {
                if (headMask.At<byte>(i, j) == 0)
                {
                    headExtractedWithAlpha.Set<Vec4b>(i, j, new Vec4b(0, 0, 0, 0));
                }
            }
        }

        // Guardar la imagen con la cabeza extraída
        Cv2.ImWrite("D://3.full.png", headExtractedWithAlpha);
    }
    static string DetermineGender(string headImagePath)
    {
        // Cargar la imagen de la cabeza
        Mat headImage = Cv2.ImRead(headImagePath);

        // Ruta al modelo preentrenado de clasificación de género
        string genderModelPath = "deploy_gender.prototxt";
        string genderWeightsPath = "gender_net.caffemodel";

        // Cargar el modelo de clasificación de género
        var genderNet = CvDnn.ReadNetFromCaffe(genderModelPath, genderWeightsPath);

        // Preprocesar la imagen de entrada
        Mat blob = CvDnn.BlobFromImage(headImage, 1.0, new OpenCvSharp.Size(227, 227), new Scalar(104, 117, 123), false, false);

        // Establecer la entrada al modelo
        genderNet.SetInput(blob);

        // Realizar la clasificación
        Mat genderPreds = genderNet.Forward();

        // Obtener la etiqueta de género
        double minVal, maxVal;
        OpenCvSharp.Point minLoc, maxLoc;
        Cv2.MinMaxLoc(genderPreds, out minVal, out maxVal, out minLoc, out maxLoc);

        int maxIdx = maxLoc.X;

        string[] genderList = { "Male", "Female" };

        return maxIdx == 0 ? "M" : "F";
    }  
}
