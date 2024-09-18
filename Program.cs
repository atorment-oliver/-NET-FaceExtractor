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
        string imagePath = "D://Ejemplo21.jpeg";
        string sexo = DetermineGender(imagePath);
        Bitmap image1 = (Bitmap)Image.FromFile(imagePath, true);

        Bitmap hairExtracted, jawExtracted, originalImage, onlyFaceExtracted;
        originalImage = (Bitmap)image1.Clone();
        using (Bitmap newBitmap = new Bitmap((Bitmap)image1.Clone()))
        {
            image1.Dispose();
            hairExtracted = ExtractHairF(newBitmap);
        }
        using (var newBitmap2 = new Bitmap((Bitmap)originalImage.Clone()))
        {
            jawExtracted = RemoveUnderJaw((Bitmap)newBitmap2.Clone());
        }

        onlyFaceExtracted = ExtractCompleteFace(jawExtracted);
        CombineHeadAndHair(onlyFaceExtracted, hairExtracted);
    }
    static void Main2(string[] args)
    {
        /*var headImage = ExtractHead();
        bool isHeadBold = IsHeadBald(headImage);
        var hairImage = ExtractHair(headImage);
        var finalImage = CombineHeadAndHair(headImage, hairImage);
        finalImage.Save("D://3.resultado_final.png", ImageFormat.Png);*/
        string imagePath = "D://YO5.jpg";
        //string outputPath = "D://4.head_extracted.png";
        //string outputPath2 = "D://2.head_extracted.png";
        string mandibulaPath = "D://2.MANIDUBLA.png";
        //string hairOutputPath = "D://4.Hair.png";
        string sexo = DetermineGender(imagePath);
        var image = Dlib.LoadImage<RgbPixel>(imagePath);
        //ExtractHair(image.ToBitmap()).Save("D://6.2.Cabello.png");
        //ExtractHair(imagePath, "D://6.1.Cabello.png");
        //ExtractHair(imagePath, "D://6.Cabello.png", true);
        ExtractHairF(imagePath, "D://7.Cabello.png");
        Bitmap image1 = (Bitmap)Image.FromFile(imagePath, true);

        //ExtractHairG(imagePath, "D://8.Cabello.png");
        //ExtractHair(imagePath, hairOutputPath);
        //ExtractHead(imagePath, outputPath);
        //image = Dlib.LoadImage<RgbPixel>(imagePath);
        //CutUnderFace(image.ToBitmap(), mandibulaPath);
        //var vHead = ExtractHead(imagePath); 
        //System.Drawing.Bitmap imageHead = 
        //ExtractCompleteFace(imagePath);
        //RemoveUnderJaw(imagePath, mandibulaPath);
        Bitmap hairExtracted, jawExtracted, originalImage;
        originalImage = (Bitmap)image1.Clone();
        using (Bitmap newBitmap = new Bitmap((Bitmap)image1.Clone()))
        {
            image1.Dispose();
            hairExtracted = ExtractHairF(newBitmap);
        }
        using (var newBitmap2 = new Bitmap((Bitmap)originalImage.Clone()))
        {
            originalImage.Dispose();
            jawExtracted = RemoveUnderJaw((Bitmap)newBitmap2.Clone());
        }

        ExtractCompleteFace(mandibulaPath);
        //if (IsHeadBald(imagePath, "D://7.Cabello.png"))
        {
            var imageHead = Dlib.LoadImage<RgbPixel>("D://3.full.png");
            var imageHair = Dlib.LoadImage<RgbPixel>("D://7.Cabello.png");
            CombineHeadAndHair("D://3.full.png", "D://7.Cabello.png", "D://9.FinalHead.png");
        }
        //else 
        //{
        //    //if (DetermineGender(imagePath).Equals("F"))
        //    {
        //        var imageHead = Dlib.LoadImage<RgbPixel>("D://3.full.png");
        //        var imageHair = Dlib.LoadImage<RgbPixel>("D://7.Cabello.png");
        //        CombineHeadAndHair("D://3.full.png", "D://7.Cabello.png", "D://9.FinalHead.png");
        //    }
        //}

        //ExtractHair(imagePath, hairOutputPath);
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
    static void ExtractHairG(string imagePath, string outputPath)
    {
        // Cargar la imagen
        Mat image = Cv2.ImRead(imagePath);
        var dlibImage = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la cara
        var points = new List<Point>();
        for (int i = 0; i < shape.Parts; i++)
        {
            var point = shape.GetPart((uint)i);
            points.Add(new Point(point.X, point.Y));
        }

        // Definir la ROI para el cabello basado en los puntos faciales
        int roiTop = Math.Max(points[19].Y - (points[8].Y - points[19].Y) * 2, 0); // Ajustar este valor según sea necesario
        int roiBottom = points[19].Y; // Punto superior de las cejas
        int roiLeft = Math.Max(points[0].X - 30, 0); // Punto más a la izquierda de la mandíbula, expandido 30 píxeles
        int roiRight = Math.Min(points[16].X + 30, image.Width); // Punto más a la derecha de la mandíbula, expandido 30 píxeles
        roiTop = Math.Max(roiTop - 30, 0); // Expandir hacia arriba 30 píxeles

        Rect roi = new Rect(roiLeft, roiTop, roiRight - roiLeft, roiBottom - roiTop);

        // Crear una submat para la ROI del cabello
        Mat hairROI = new Mat(image, roi);

        // Convertir la ROI a espacio de color HSV
        Mat hsvImage = new Mat();
        Cv2.CvtColor(hairROI, hsvImage, ColorConversionCodes.BGR2HSV);

        // Definir el rango de color para el cabello (ajustar estos valores según el color del cabello)
        Scalar lower = new Scalar(0, 0, 0); // Cambiar estos valores según el color del cabello
        Scalar upper = new Scalar(179, 255, 80); // Cambiar estos valores según el color del cabello

        // Crear una máscara basada en el rango de color del cabello
        Mat mask = new Mat();
        Cv2.InRange(hsvImage, lower, upper, mask);

        // Aplicar operaciones morfológicas para limpiar la máscara
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

        // Crear una imagen con fondo transparente
        Mat hairExtracted = new Mat(image.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar solo el cabello a la imagen con fondo transparente
        for (int y = 0; y < mask.Rows; y++)
        {
            for (int x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) != 0)
                {
                    Vec3b color = hairROI.At<Vec3b>(y, x);
                    hairExtracted.Set(y + roiTop, x + roiLeft, new Vec4b(color.Item0, color.Item1, color.Item2, 255));
                }
            }
        }

        // Guardar la imagen con el cabello extraído
        Cv2.ImWrite(outputPath, hairExtracted);
    }
    static void ExtractHairF(string imagePath, string outputPath)
    {
        // Cargar la imagen
        Mat image = Cv2.ImRead(imagePath);
        var dlibImage = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la cara
        var points = new List<Point>();
        for (int i = 0; i < shape.Parts; i++)
        {
            var point = shape.GetPart((uint)i);
            points.Add(new Point(point.X, point.Y));
        }

        // Definir la ROI para el cabello basado en los puntos faciales
        int roiTop = Math.Max(points[19].Y - (points[8].Y - points[19].Y) * 2, 0); // Ajustar este valor según sea necesario
        int roiBottom = points[4].Y; // Punto de la barbilla
        int roiLeft = Math.Max(points[0].X - 30, 0); // Punto más a la izquierda de la mandíbula, expandido 30 píxeles
        int roiRight = Math.Min(points[16].X + 30, image.Width); // Punto más a la derecha de la mandíbula, expandido 30 píxeles
        roiTop = Math.Max(roiTop - 30, 0); // Expandir hacia arriba 30 píxeles

        Rect roi = new Rect(roiLeft, roiTop, roiRight - roiLeft, roiBottom - roiTop);

        // Crear una submat para la ROI del cabello
        Mat hairROI = new Mat(image, roi);

        // Convertir la ROI a espacio de color HSV
        Mat hsvImage = new Mat();
        Cv2.CvtColor(hairROI, hsvImage, ColorConversionCodes.BGR2HSV);

        // Definir el rango de color para el cabello (ajustar estos valores según el color del cabello)
        Scalar lower = new Scalar(0, 0, 0); // Cambiar estos valores según el color del cabello
        Scalar upper = new Scalar(179, 255, 80); // Cambiar estos valores según el color del cabello

        // Crear una máscara basada en el rango de color del cabello
        Mat mask = new Mat();
        Cv2.InRange(hsvImage, lower, upper, mask);

        // Aplicar operaciones morfológicas para limpiar la máscara
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

        // Crear una imagen con fondo transparente
        Mat hairExtracted = new Mat(image.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar solo el cabello a la imagen con fondo transparente
        for (int y = 0; y < mask.Rows; y++)
        {
            for (int x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) != 0)
                {
                    Vec3b color = hairROI.At<Vec3b>(y, x);
                    hairExtracted.Set(y + roiTop, x + roiLeft, new Vec4b(color.Item0, color.Item1, color.Item2, 255));
                }
            }
        }

        // Guardar la imagen con el cabello extraído
        Cv2.ImWrite(outputPath, hairExtracted);
    }
    static Bitmap ExtractHairF(Bitmap bitmap)
    {
        Mat image = BitmapConverter.ToMat(bitmap);

        var dlibImage = Util.ToArray2D(bitmap);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return null;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la cara
        var points = new List<Point>();
        for (int i = 0; i < shape.Parts; i++)
        {
            var point = shape.GetPart((uint)i);
            points.Add(new Point(point.X, point.Y));
        }

        // Definir la ROI para el cabello basado en los puntos faciales
        int roiTop = Math.Max(points[19].Y - (points[8].Y - points[19].Y) * 2, 0); // Ajustar este valor según sea necesario
        int roiBottom = points[4].Y; // Punto de la barbilla
        int roiLeft = Math.Max(points[0].X - 30, 0); // Punto más a la izquierda de la mandíbula, expandido 30 píxeles
        int roiRight = Math.Min(points[16].X + 30, image.Width); // Punto más a la derecha de la mandíbula, expandido 30 píxeles
        roiTop = Math.Max(roiTop - 30, 0); // Expandir hacia arriba 30 píxeles

        Rect roi = new Rect(roiLeft, roiTop, roiRight - roiLeft, roiBottom - roiTop);

        // Crear una submat para la ROI del cabello
        Mat hairROI = new Mat(image, roi);

        // Convertir la ROI a espacio de color HSV
        Mat hsvImage = new Mat();
        Cv2.CvtColor(hairROI, hsvImage, ColorConversionCodes.BGR2HSV);

        // Definir el rango de color para el cabello (ajustar estos valores según el color del cabello)
        Scalar lower = new Scalar(0, 0, 0); // Cambiar estos valores según el color del cabello
        Scalar upper = new Scalar(179, 255, 80); // Cambiar estos valores según el color del cabello

        // Crear una máscara basada en el rango de color del cabello
        Mat mask = new Mat();
        Cv2.InRange(hsvImage, lower, upper, mask);

        // Aplicar operaciones morfológicas para limpiar la máscara
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

        // Crear una imagen con fondo transparente
        Mat hairExtracted = new Mat(image.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar solo el cabello a la imagen con fondo transparente
        for (int y = 0; y < mask.Rows; y++)
        {
            for (int x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) != 0)
                {
                    Vec3b color = hairROI.At<Vec3b>(y, x);
                    hairExtracted.Set(y + roiTop, x + roiLeft, new Vec4b(color.Item0, color.Item1, color.Item2, 255));
                }
            }
            //Cv2.ImWrite("D://7"+ y.ToString() +".0.Cabello.png", hairExtracted);
        }

        // Guardar la imagen con el cabello extraído

        //Cv2.ImWrite("D://7.Cabello.png", hairExtracted);
        return hairExtracted.ToBitmap();
    }
    static void RemoveUnderJaw(Bitmap croppedHead, string outputPath)
    {
        // Cargar la imagen
        //Bitmap bitmap = new Bitmap(imagePath);
        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, croppedHead.Width, croppedHead.Height);
        System.Drawing.Imaging.BitmapData data = croppedHead.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, croppedHead.PixelFormat);

        var array = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, array, 0, array.Length);

        var dlibImage = Dlib.LoadImageData<RgbPixel>(array, (uint)croppedHead.Height, (uint)croppedHead.Width, (uint)data.Stride);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la mandíbula
        var jawPoints = new List<Point>();
        for (int i = 4; i <= 14; i++)
        {
            var point = shape.GetPart((uint)i);
            jawPoints.Add(new Point(point.X, point.Y));
        }

        // Definir los puntos adicionales para cerrar la región de la mandíbula
        int imageWidth = croppedHead.Width;
        int imageHeight = croppedHead.Height;
        jawPoints.Add(new Point(imageWidth, imageHeight));
        jawPoints.Add(new Point(0, imageHeight));
        jawPoints.Add(new Point(jawPoints[0].X, jawPoints[0].Y));

        // Crear una máscara con la región de la mandíbula
        using (Graphics g = Graphics.FromImage(croppedHead))
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(jawPoints.ToArray());
                using (Region region = new Region(path))
                {
                    g.SetClip(region, CombineMode.Replace);
                    g.Clear(Color.Transparent);
                }
            }
        }

        // Guardar la imagen con la mandíbula recortada
        croppedHead.Save(outputPath, ImageFormat.Png);
    }
    static void RemoveUnderJaw(string imagePath, string outputPath)
    {
        // Cargar la imagen
        Bitmap bitmap = new Bitmap(imagePath);
        var dlibImage = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la mandíbula
        var jawPoints = new List<Point>();
        for (int i = 4; i <= 12; i++)
        {
            var point = shape.GetPart((uint)i);
            jawPoints.Add(new Point(point.X, point.Y));
        }

        // Definir los puntos adicionales para cerrar la región de la mandíbula
        int imageWidth = bitmap.Width;
        int imageHeight = bitmap.Height;
        jawPoints.Add(new Point(imageWidth, imageHeight));
        jawPoints.Add(new Point(0, imageHeight));
        jawPoints.Add(new Point(jawPoints[0].X, jawPoints[0].Y));

        // Crear una máscara con la región de la mandíbula
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(jawPoints.ToArray());
                using (Region region = new Region(path))
                {
                    g.SetClip(region, CombineMode.Replace);
                    g.Clear(Color.Transparent);
                }
            }
        }

        // Guardar la imagen con la mandíbula recortada
        bitmap.Save(outputPath, ImageFormat.Png);
    }
    static Bitmap RemoveUnderJaw(Bitmap bitmap)
    {
        var dlibImage = Util.ToArray2D(bitmap);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return null;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la mandíbula
        var jawPoints = new List<Point>();
        for (int i = 4; i <= 12; i++)
        {
            var point = shape.GetPart((uint)i);
            jawPoints.Add(new Point(point.X, point.Y));
        }

        // Definir los puntos adicionales para cerrar la región de la mandíbula
        int imageWidth = bitmap.Width;
        int imageHeight = bitmap.Height;
        jawPoints.Add(new Point(imageWidth, imageHeight));
        jawPoints.Add(new Point(0, imageHeight));
        jawPoints.Add(new Point(jawPoints[0].X, jawPoints[0].Y));
        // Crear una máscara con la región de la mandíbula
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(jawPoints.ToArray());
                using (Region region = new Region(path))
                {
                    g.SetClip(region, CombineMode.Replace);
                    g.Clear(Color.Transparent);
                }
            }
        }
        // Guardar la imagen con la mandíbula recortada
        //bitmap.Save("D://2.1.MANIDUBLA.png", ImageFormat.Png);
        return bitmap;
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
        //returnHead = (System.Drawing.Bitmap)OpenCvSharp.Extensions.BitmapConverter.ToBitmap(headExtractedWithAlpha);
        //return returnHead;
    }
    static Bitmap ExtractCompleteFace(Bitmap underjawImage)
    {
        Mat image = BitmapConverter.ToMat((Bitmap)underjawImage.Clone());
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
        //Cv2.ImWrite("D://3.full.png", headExtractedWithAlpha);
        return headExtractedWithAlpha.ToBitmap();
    }
    static void ExtractHead(string imagePath, string outputPath)
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
        Cv2.ImWrite(outputPath, headExtractedWithAlpha);
    }
    static Bitmap ExtractHead(string imagePath)
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
        return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(headExtractedWithAlpha);
    }
    static Bitmap CutUnderFace(Bitmap croppedHead, string output)
    {
        Bitmap returnHead = null;
        //var dlibImage = Dlib.LoadImage<RgbPixel>(croppedHead);
        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, croppedHead.Width, croppedHead.Height);
        System.Drawing.Imaging.BitmapData data = croppedHead.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, croppedHead.PixelFormat);

        var array = new byte[data.Stride * data.Height];
        Marshal.Copy(data.Scan0, array, 0, array.Length);

        var image = Dlib.LoadImageData<RgbPixel>(array, (uint)croppedHead.Height, (uint)croppedHead.Width, (uint)data.Stride);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(image);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return null;
        }
        using (Graphics graphics = Graphics.FromImage(croppedHead))
        {
            if (faces.Length > 0)
            {
                // Tomar el primer rostro detectado
                var face = faces[0];

                // Predecir los puntos faciales
                var shape = shapePredictor.Detect(image, face);

                // Obtener los puntos del contorno de la cara
                var points = new List<Point>();
                for (int i = 0; i < shape.Parts; i++)
                {
                    var point = shape.GetPart((uint)i);
                    points.Add(new Point(point.X, point.Y));
                }

                // Crear una máscara para el rostro
                using (Bitmap mask = new Bitmap(image.Columns, image.Rows))
                using (Graphics g = Graphics.FromImage(mask))
                {
                    g.Clear(Color.Black);

                    // Dibujar el contorno del rostro y la cabeza
                    var headOutline = new List<Point>();

                    // Agregar puntos de la mandíbula (0 a 16)
                    for (int i = 0; i <= 16; i++)
                    {
                        headOutline.Add(points[i]);
                    }
                    g.FillPolygon(Brushes.White, headOutline.ToArray());

                    using (Bitmap originalImage = image.ToBitmap())
                    {

                        using (Graphics gCropped = Graphics.FromImage(originalImage))
                        {
                            for (int x = 0; x < originalImage.Width; x++)
                            {
                                // Aplicar la máscara
                                for (int y = 0; y < originalImage.Height; y++)
                                {

                                    foreach (Point puntoMandibula in headOutline)
                                    {
                                        if (y < puntoMandibula.Y)
                                        {
                                            originalImage.SetPixel(x, y, Color.Black);
                                        }
                                    }
                                }
                            }
                            originalImage.Save(output, ImageFormat.Png);
                            returnHead = (Bitmap)originalImage.Clone();
                        }
                    }
                }
            }
        }
        return returnHead;
    }
    static void ExtractHair(string imagePath, string outputPath)
    {
        // Cargar la imagen
        Mat image = Cv2.ImRead(imagePath);
        var dlibImage = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos del contorno de la cara
        var points = new List<Point>();
        for (int i = 0; i < shape.Parts; i++)
        {
            var point = shape.GetPart((uint)i);
            points.Add(new Point(point.X, point.Y));
        }

        // Definir la ROI para el cabello basado en los puntos faciales
        int roiTop = Math.Max(points[19].Y - (points[8].Y - points[19].Y) * 2, 0); // Adjust this value as necessary
        int roiBottom = points[8].Y; // Chin point
        int roiLeft = points[0].X; // Leftmost jaw point
        int roiRight = points[16].X; // Rightmost jaw point
        Rect roi = new Rect(roiLeft, roiTop, roiRight - roiLeft, roiBottom - roiTop);

        // Crear una submat para la ROI del cabello
        Mat hairROI = new Mat(image, roi);

        // Convertir la ROI a espacio de color HSV
        Mat hsvImage = new Mat();
        Cv2.CvtColor(hairROI, hsvImage, ColorConversionCodes.BGR2HSV);

        // Definir el rango de color para el cabello (ajustar estos valores según el color del cabello)
        Scalar lower = new Scalar(0, 0, 0); // Cambia estos valores según el color del cabello
        Scalar upper = new Scalar(179, 255, 80); // Cambia estos valores según el color del cabello

        // Crear una máscara basada en el rango de color del cabello
        Mat mask = new Mat();
        Cv2.InRange(hsvImage, lower, upper, mask);

        // Aplicar operaciones morfológicas para limpiar la máscara
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
        Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

        // Crear una imagen con fondo transparente
        Mat hairExtracted = new Mat(image.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar solo el cabello a la imagen con fondo transparente
        for (int y = 0; y < mask.Rows; y++)
        {
            for (int x = 0; x < mask.Cols; x++)
            {
                if (mask.At<byte>(y, x) != 0)
                {
                    Vec3b color = hairROI.At<Vec3b>(y, x);
                    hairExtracted.Set(y + roiTop, x + roiLeft, new Vec4b(color.Item0, color.Item1, color.Item2, 255));
                }
            }
        }

        // Guardar la imagen con el cabello extraído
        Cv2.ImWrite(outputPath, hairExtracted);
    }
    static Bitmap ExtractHead()
    {
        Bitmap headCropped = null;
        // Ruta al archivo de la imagen
        string imagePath = "D://YO3.jpg";

        // Cargar la imagen
        var image = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)?.Replace("file:\\", "") + "\\shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros
        var detector = Dlib.GetFrontalFaceDetector();

        // Crear el predictor de puntos faciales
        var shapePredictor = new ShapePredictor();
        shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(image);

        // Crear un bitmap para dibujar los contornos
        using (Bitmap bitmap = image.ToBitmap())
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            if (faces.Length > 0)
            {
                // Tomar el primer rostro detectado
                var face = faces[0];

                // Predecir los puntos faciales
                var shape = shapePredictor.Detect(image, face);

                // Obtener los puntos del contorno de la cara
                var points = new List<Point>();
                for (int i = 0; i < shape.Parts; i++)
                {
                    var point = shape.GetPart((uint)i);
                    points.Add(new Point(point.X, point.Y));
                }

                // Crear una máscara para el rostro
                using (Bitmap mask = new Bitmap(image.Columns, image.Rows))
                using (Graphics g = Graphics.FromImage(mask))
                {
                    g.Clear(Color.Black);

                    // Dibujar el contorno del rostro y la cabeza
                    var headOutline = new List<Point>();

                    // Agregar puntos de la mandíbula (0 a 16)
                    for (int i = 0; i <= 16; i++)
                    {
                        headOutline.Add(points[i]);
                    }

                    // Estimar la parte superior de la cabeza
                    int leftCheekX = points[8].X - 550;
                    int rightCheekX = points[10].X + 550;
                    int topY = points[17].Y - (points[8].Y - points[17].Y) * 2; // Ajuste de la altura basado en la frente

                    headOutline.Insert(0, new Point(leftCheekX, topY));
                    headOutline.Add(new Point(rightCheekX, topY));

                    // Rellenar el contorno del rostro y la cabeza
                    g.FillPolygon(Brushes.White, headOutline.ToArray());

                    // Recortar el rostro y la cabeza usando la máscara
                    using (Bitmap originalImage = image.ToBitmap())
                    {
                        // Crear un rectángulo que cubra la cabeza y el rostro
                        int top = Math.Max(0, topY);
                        int bottom = Math.Min(image.Rows, points[8].Y); // punto más bajo de la barbilla
                        int left = (int)Math.Max(0, face.Left - (face.Width / 2)); // expandir a los lados
                        int right = (int)Math.Min(image.Columns, face.Right + (face.Width / 2)); // expandir a los lados
                        int width = right - left;
                        int height = bottom - top;

                        using (Bitmap croppedFace = new Bitmap(width, height))
                        {
                            using (Graphics gCropped = Graphics.FromImage(croppedFace))
                            {
                                gCropped.Clear(Color.Transparent);

                                // Convertir DlibDotNet.Rectangle a System.Drawing.Rectangle
                                var rect = new System.Drawing.Rectangle(left, top, width, height);
                                gCropped.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, width, height), rect, GraphicsUnit.Pixel);

                                // Aplicar la máscara
                                for (int y = 0; y < height; y++)
                                {
                                    for (int x = 0; x < width; x++)
                                    {
                                        if (mask.GetPixel(left + x, top + y).R == 0)
                                        {
                                            croppedFace.SetPixel(x, y, Color.Transparent);
                                        }
                                    }
                                }
                            }

                            // Guardar la imagen del rostro y la cabeza
                            croppedFace.Save("d://1.rostro_y_cabeza_extraido.png", ImageFormat.Png);
                            headCropped = (Bitmap)croppedFace.Clone();
                        }
                    }
                }
            }
        }
        return headCropped;
    }

    static Bitmap ExtractHair(Bitmap image)
    {
        // Ruta al archivo de la imagen
        //string imagePath = "D:\\1.rostro_y_cabeza_extraido.png";
        Bitmap hairOnlyCropped = null;
        // Cargar la imagen
        using (Bitmap originalImage = new Bitmap(image))
        {
            // Crear una máscara para el cabello
            using (Bitmap hairMask = new Bitmap(originalImage.Width, originalImage.Height))
            {
                // Crear la imagen resultante con solo el cabello
                using (Bitmap hairOnly = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb))
                {
                    int midFaceY = (originalImage.Height) / 1; // Punto medio del rostro (ajustar según sea necesario)

                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        for (int x = 0; x < originalImage.Width; x++)
                        {
                            Color pixelColor = originalImage.GetPixel(x, y);
                            if (y <= midFaceY && IsHairColor(pixelColor))
                            {
                                hairOnly.SetPixel(x, y, pixelColor);
                            }
                            else
                            {
                                hairOnly.SetPixel(x, y, Color.Transparent);
                            }
                        }
                    }

                    // Guardar la imagen con solo el cabello
                    hairOnly.Save("D://5.solo_cabello.png", ImageFormat.Png);
                    hairOnlyCropped = (Bitmap)hairOnly.Clone();
                }
            }
        }
        return hairOnlyCropped;
    }
    static void ExtractHair(string imagePath, string outputPath, bool estr)
    {
        // Cargar la imagen
        Mat image = Cv2.ImRead(imagePath);
        var dlibImage = Dlib.LoadImage<RgbPixel>(imagePath);

        // Ruta al modelo preentrenado de predicción de puntos faciales
        string modelPath = "shape_predictor_68_face_landmarks.dat";

        // Crear el detector de rostros y el predictor de puntos faciales
        var detector = Dlib.GetFrontalFaceDetector();
        var shapePredictor = ShapePredictor.Deserialize(modelPath);

        // Detectar rostros en la imagen
        var faces = detector.Operator(dlibImage);

        if (faces.Length == 0)
        {
            Console.WriteLine("No face detected.");
            return;
        }

        // Usar el primer rostro detectado
        var face = faces[0];
        var shape = shapePredictor.Detect(dlibImage, face);

        // Obtener los puntos faciales que delimitan la región del cabello
        var hairPoints = new List<Point>();
        for (int i = 17; i <= 26; i++) // Puntos de la frente
        {
            var point = shape.GetPart((uint)i);
            hairPoints.Add(new Point(point.X, point.Y));
        }

        // Añadir puntos adicionales para cerrar la región del cabello
        hairPoints.Add(new Point(hairPoints[0].X, 0)); // Punto en la parte superior izquierda
        hairPoints.Add(new Point(hairPoints[hairPoints.Count - 2].X, 0)); // Punto en la parte superior derecha

        // Crear una máscara con la región del cabello
        Mat mask = new Mat(image.Size(), MatType.CV_8UC1, Scalar.Black);
        var hairPointsCv = new List<OpenCvSharp.Point>();
        foreach (var pt in hairPoints)
        {
            hairPointsCv.Add(new OpenCvSharp.Point(pt.X, pt.Y));
        }
        Cv2.FillConvexPoly(mask, hairPointsCv, Scalar.White);

        // Crear una imagen con solo el cabello
        Mat hairImage = new Mat(image.Size(), MatType.CV_8UC4, Scalar.All(0));
        image.CopyTo(hairImage, mask);

        // Convertir la imagen de 3 canales a 4 canales para la transparencia
        Cv2.CvtColor(hairImage, hairImage, ColorConversionCodes.BGR2BGRA);

        // Aplicar la transparencia a los píxeles que no son parte del cabello
        for (int i = 0; i < hairImage.Rows; i++)
        {
            for (int j = 0; j < hairImage.Cols; j++)
            {
                if (mask.At<byte>(i, j) == 0)
                {
                    hairImage.Set(i, j, new Vec4b(0, 0, 0, 0));
                }
            }
        }

        // Guardar la imagen con el cabello extraído
        Cv2.ImWrite(outputPath, hairImage);
    }
    static bool IsHeadBald(Bitmap headImage)
    {
        // Asumimos que la cabeza es calva hasta que se demuestre lo contrario
        bool isBald = true;
        int midFaceY = headImage.Height / 2;

        // Contadores de píxeles de piel y no piel
        int skinPixelCount = 0;
        int nonSkinPixelCount = 0;

        // Analizar la parte superior de la cabeza
        for (int y = 0; y < midFaceY; y++)
        {
            for (int x = 0; x < headImage.Width; x++)
            {
                Color pixelColor = headImage.GetPixel(x, y);
                if (IsSkinColor(pixelColor))
                {
                    skinPixelCount++;
                }
                else
                {
                    nonSkinPixelCount++;
                }
            }
        }

        // Decidir si la cabeza es calva basándose en el ratio de píxeles de piel
        double skinPixelRatio = (double)skinPixelCount / (skinPixelCount + nonSkinPixelCount);
        isBald = skinPixelRatio > 0.8; // Si más del 80% de los píxeles son de piel, se considera calvo

        return isBald;
    }
    static bool IsSkinColor(Color color)
    {
        // Definir los rangos de color de la piel (ajustar según los colores de piel)
        var skinColorRanges = new List<(int minR, int maxR, int minG, int maxG, int minB, int maxB)>
        {
            (45, 255, 34, 255, 30, 255), // Primer rango de color de piel
            // Agregar más rangos si es necesario
        };

        foreach (var range in skinColorRanges)
        {
            if (color.R >= range.minR && color.R <= range.maxR &&
                color.G >= range.minG && color.G <= range.maxG &&
                color.B >= range.minB && color.B <= range.maxB)
            {
                return true;
            }
        }
        return false;
    }
    /*static bool IsHairColor(Color color)
    {
        // Definir el rango de color del cabello (ajustar según el color del cabello)
        int minR = 0, maxR = 100;
        int minG = 0, maxG = 100;
        int minB = 0, maxB = 100;

        return color.R >= minR && color.R <= maxR &&
               color.G >= minG && color.G <= maxG &&
               color.B >= minB && color.B <= maxB;
    }*/
    static bool IsHairColor(Color color)
    {
        // Definir los rangos de color del cabello (ajustar según los colores del cabello)
        var hairColorRanges = new List<(int minR, int maxR, int minG, int maxG, int minB, int maxB)>
        {
            /*(0, 25, 0, 25, 0, 25),
            (0, 50, 0, 50, 0, 51),// Primer rango de color
            (51, 101, 50, 100, 50, 100),
            (101, 150, 50, 100, 50, 100), // Segundo rango de color
            (1, 200, 100, 150, 100, 150) // Tercer rango de color
            */
            
            (0, 100, 0, 100, 0, 100), // Primer rango de color
            (101, 150, 50, 100, 50, 100), // Segundo rango de color
            (151, 200, 100, 150, 100, 150) // Tercer rango de color
            
        };

        foreach (var range in hairColorRanges)
        {
            if (color.R >= range.minR && color.R <= range.maxR &&
                color.G >= range.minG && color.G <= range.maxG &&
                color.B >= range.minB && color.B <= range.maxB)
            {
                return true;
            }
        }
        return false;
    }
    static void CombineHeadAndHair(string headImagePath, string hairImagePath, string outputPath)
    {
        // Cargar las imágenes de la cabeza y el cabello
        Mat headImage = Cv2.ImRead(headImagePath, ImreadModes.Unchanged);
        Mat hairImage = Cv2.ImRead(hairImagePath, ImreadModes.Unchanged);

        // Crear una imagen combinada con fondo transparente
        Mat combinedImage = new Mat(headImage.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar la cabeza a la imagen combinada
        for (int y = 0; y < headImage.Rows; y++)
        {
            for (int x = 0; x < headImage.Cols; x++)
            {
                Vec4b headPixel = headImage.At<Vec4b>(y, x);
                if (headPixel[3] != 0) // Check if the pixel is not transparent
                {
                    combinedImage.Set(y, x, headPixel);
                }
            }
        }

        // Superponer el cabello a la imagen combinada
        for (int y = 0; y < hairImage.Rows; y++)
        {
            for (int x = 0; x < hairImage.Cols; x++)
            {
                Vec4b hairPixel = hairImage.At<Vec4b>(y, x);
                if (hairPixel[3] != 0) // Check if the pixel is not transparent
                {
                    combinedImage.Set(y, x, hairPixel);
                }
            }
        }

        // Guardar la imagen combinada
        Cv2.ImWrite(outputPath, combinedImage);
    }
    static Bitmap CombineHeadAndHair(Bitmap fullHead, Bitmap onlyHair)
    {
        // Cargar las imágenes de la cabeza y el cabello
        Mat headImage = BitmapConverter.ToMat((Bitmap)fullHead.Clone());
        Mat hairImage = BitmapConverter.ToMat((Bitmap)onlyHair.Clone());

        // Crear una imagen combinada con fondo transparente
        Mat combinedImage = new Mat(headImage.Size(), MatType.CV_8UC4, Scalar.All(0));

        // Copiar la cabeza a la imagen combinada
        for (int y = 0; y < headImage.Rows; y++)
        {
            for (int x = 0; x < headImage.Cols; x++)
            {
                Vec4b headPixel = headImage.At<Vec4b>(y, x);
                if (headPixel[3] != 0) // Check if the pixel is not transparent
                {
                    combinedImage.Set(y, x, headPixel);
                }
            }
        }

        // Superponer el cabello a la imagen combinada
        for (int y = 0; y < hairImage.Rows; y++)
        {
            for (int x = 0; x < hairImage.Cols; x++)
            {
                Vec4b hairPixel = hairImage.At<Vec4b>(y, x);
                if (hairPixel[3] != 0) // Check if the pixel is not transparent
                {
                    combinedImage.Set(y, x, hairPixel);
                }
            }
        }

        // Guardar la imagen combinada
        Cv2.ImWrite("D:\\9_" + Guid.NewGuid().ToString() + ".FULL.png", combinedImage);
        return combinedImage.ToBitmap();
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
    static Bitmap ResizeImageIfNeeded(Bitmap originalImage, int maxWidth, int maxHeight)
    {
        int originalWidth = originalImage.Width;
        int originalHeight = originalImage.Height;

        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            // La imagen ya está dentro de los límites, no se necesita redimensionar
            return new Bitmap(originalImage);
        }

        // Calcular el nuevo tamaño manteniendo la relación de aspecto
        float ratioX = (float)maxWidth / originalWidth;
        float ratioY = (float)maxHeight / originalHeight;
        float ratio = Math.Min(ratioX, ratioY);

        int newWidth = (int)(originalWidth * ratio);
        int newHeight = (int)(originalHeight * ratio);

        // Crear una nueva imagen redimensionada
        Bitmap resizedImage = new Bitmap(newWidth, newHeight);
        using (Graphics g = Graphics.FromImage(resizedImage))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
        }

        return resizedImage;
    }

}
