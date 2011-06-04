﻿/*
 * Created by SharpDevelop.
 * User: Peter Forstmeier
 * Date: 10.05.2011
 * Time: 19:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using ICSharpCode.Reports.Core.Exporter;
using ICSharpCode.Reports.Core.Globals;

namespace ICSharpCode.Reports.Core.WPF
{
	/// <summary>
	/// Description of FixedDocumentCreator.
	/// </summary>
	public class FixedDocumentCreator
	{
		
		BrushConverter brushConverter ;
		
		public FixedDocumentCreator()
		{
			brushConverter = new BrushConverter();
		}
		
		
		public FixedPage CreatePage(ExporterPage exporterPage)
		{
			FixedPage page = new FixedPage();
			CreatePageInternal (page,exporterPage.Items);
			return page;
		}
		
		
		void CreatePageInternal(FixedPage page, ExporterCollection items)
		{
			foreach (var element in items)
			{
				var item = ItemFactory(element);
				if (item != null) {
					FixedPage.SetLeft(item,element.StyleDecorator.Location.X );
					FixedPage.SetTop(item,element.StyleDecorator.Location.Y);
					page.Children.Add(item);
				}
			}
		}
		
		
		UIElement ItemFactory (BaseExportColumn column)
		{
			UIElement element = null;
			System.Windows.Controls.Border border = null;
			
			var graphicContainer = column as ExportGraphicContainer;
			if ( graphicContainer != null) {
				element = CreateGraphicsContainer(graphicContainer);
				return element;
			}
			
			var container = column as ExportContainer;
			if (container != null) {
				element = CreateContainer(container);
			}
			
			var exportGraphic = column as ExportGraphic;
			if (exportGraphic != null) {
				element = CreateGraphicsElement(exportGraphic);
			}
			
			var text = column as ExportText;
			if (text != null) {
				var t = CreateTextBlock(text);
				if (column.StyleDecorator.DrawBorder) {
					border = CreateBorder(column.StyleDecorator as BaseStyleDecorator);
					border.Child = t;
					element = border;
				} 
				else
				{
					element = t;
				}
			}
			

			var image = column as ExportImage;
			
			if (image != null)
			{
				element = CreateImageColumn(image);
			}
			
			return element;
		}
		
	
		#region GraphicsElement (Line etc)
		
		System.Windows.Controls.Border CreateBorder( BaseStyleDecorator column)
		{
			var border = new System.Windows.Controls.Border();
			border.Padding = new Thickness(1);
			border.BorderThickness = new Thickness(2);
			border.CornerRadius = new CornerRadius(2);
			border.BorderBrush = brushConverter.ConvertFromString(column.ForeColor.Name) as SolidColorBrush;
			return border;
		}
		
		
		UIElement CreateGraphicsElement(ExportGraphic column)
		{
			var  line = new System.Windows.Shapes.Line();
			line.Stroke = brushConverter.ConvertFromString(column.StyleDecorator.ForeColor.Name) as SolidColorBrush;
			line.StrokeThickness = column.StyleDecorator.Thickness;
			
			var ld = column.StyleDecorator as LineDecorator;
			
			if (ld != null) {
				line.X1 = ld.From.X;
				line.Y1 = ld.From.Y;
				line.X2 = ld.To.X;
				line.Y2 = ld.To.Y;
			} else {
				line.X1 = column.StyleDecorator.Location.X;
				line.Y1 = column.StyleDecorator.Location.Y;
				line.X2 = column.StyleDecorator.DisplayRectangle.Width;
				line.Y2 = column.StyleDecorator.Location.Y;
			}
			return line;
		}
		
		#endregion
		
		#region Container 
		UIElement CreateGraphicsContainer(ExportGraphicContainer graphicContainer)
		{
			Console.WriteLine("GraphicContainer");
			IGraphicStyleDecorator decorator = graphicContainer.StyleDecorator as IGraphicStyleDecorator;
			UIElement shape = null;	
			var ss = decorator.Shape as EllipseShape;
			
			if (ss != null) {
				
				var circle  = new System.Windows.Shapes.Ellipse();
				SetDimension(circle,decorator);
				circle.Fill = brushConverter.ConvertFromString(decorator.BackColor.Name) as SolidColorBrush;
				circle.StrokeThickness = decorator.Thickness;
				circle.Stroke = brushConverter.ConvertFromString(decorator.ForeColor.Name) as SolidColorBrush;
				shape = circle;
				
				
			}
			else
				
			{
				
				var border = CreateBorder(decorator as BaseStyleDecorator);
				SetDimension(border,decorator);
				
				RectangleShape rs = decorator.Shape as RectangleShape;
				
				border.CornerRadius = new CornerRadius(rs.CornerRadius);
				border.BorderThickness = new Thickness(decorator.Thickness);
				border.BorderBrush = brushConverter.ConvertFromString(decorator.ForeColor.Name) as SolidColorBrush;
				shape = border;
			}
		
			
			return shape;
		}
		
		
		
		private UIElement CreateContainer(ExportContainer container)
		{
			Console.WriteLine("CreateContainer");
			var canvas = new Canvas();
			SetDimension(canvas,container.StyleDecorator);

			SolidColorBrush backgroundBrush = brushConverter.ConvertFromString(container.StyleDecorator.BackColor.Name) as SolidColorBrush;
			canvas.Background = backgroundBrush;
			
			foreach (var exportElement in container.Items) {
				var uiElement = ItemFactory (exportElement);
				Canvas.SetLeft(uiElement,exportElement.StyleDecorator.Location.X - container.StyleDecorator.Location.X);
				Canvas.SetTop(uiElement,exportElement.StyleDecorator.Location.Y - container.StyleDecorator.Location.Y);
				canvas.Children.Add(uiElement);
			}
			
			canvas.Measure(PageSize);
			canvas.Arrange(new Rect(new System.Windows.Point(), PageSize));
			canvas.UpdateLayout();
			return canvas;
		}
		
		#endregion
		
		/*
		void CreateShape(Canvas canvas, IGraphicStyleDecorator bs)
		{
			var shape = bs.Shape;
			UIElement element = null;
			if (shape is RectangleShape) {
				element = new System.Windows.Controls.Border();
				
			}
			
			if (element != null) {
				canvas.Children.Add(element);
				Canvas.SetLeft(element,bs.Location.X);
				Canvas.SetTop(element,bs.Location.Y);
			}
		}
*/

		
		UIElement CreateTextColumn(ExportText et)
		{
			TextBlock tb = CreateTextBlock(et);
			return tb;
		}

		
		#region Image
		
		UIElement CreateImageColumn(ExportImage exportImage)
		{
			System.Windows.Media.Imaging.BitmapImage bitmap = BitmapFromImage(exportImage);
			Image image = new Image();
			image.Source = bitmap;
			SetDimension(image,exportImage.StyleDecorator);
			image.Stretch = System.Windows.Media.Stretch.Fill;
			return image;
		}

		
		System.Windows.Media.Imaging.BitmapImage BitmapFromImage(ExportImage image)
		{
			var bitmap = new System.Windows.Media.Imaging.BitmapImage();
			bitmap.BeginInit();
			MemoryStream memoryStream = new MemoryStream();
			image.Image.Save(memoryStream, ImageFormat.Bmp);
			memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
			bitmap.StreamSource = memoryStream;
			bitmap.EndInit();
			return bitmap;
		}
		
		#endregion
		
		#region TextBlock
		
		TextBlock CreateTextBlock(ExportText exportText)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = exportText.Text;
			SetFont(textBlock, exportText.StyleDecorator);
			SetDimension(textBlock,exportText.StyleDecorator);
			return textBlock;
		}
		
		
		
		void SetFont(TextBlock textBlock, TextStyleDecorator styleDecorator)
		{
			textBlock.FontFamily = new FontFamily(styleDecorator.Font.FontFamily.Name);
			var b = styleDecorator.Font.Size;
			textBlock.FontSize = b * 96/72;
			textBlock.Foreground = brushConverter.ConvertFromString(styleDecorator.ForeColor.Name) as SolidColorBrush;
			if (styleDecorator.Font.Bold) {
				textBlock.FontWeight = FontWeights.Bold;
			}
			if (styleDecorator.Font.Underline) {
				CreateUnderline(textBlock,styleDecorator);
			}
			
			if (styleDecorator.Font.Italic) {
				textBlock.FontStyle = System.Windows.FontStyles.Italic ;
			}
			if (styleDecorator.Font.Strikeout) {
				CreateStrikeout(textBlock,styleDecorator);
			}
		}
		
		
		void CreateStrikeout (TextBlock textBlock, TextStyleDecorator styleDecorator)
		{
			TextDecoration strikeOut = new TextDecoration();
			strikeOut.Location = TextDecorationLocation.Strikethrough;

			Pen p = CreateWpfPen(styleDecorator);
			strikeOut.Pen = p ;
			strikeOut.PenThicknessUnit = TextDecorationUnit.FontRecommended;
			textBlock.TextDecorations.Add(strikeOut);
		}
		
		
		void CreateUnderline(TextBlock textBlock,TextStyleDecorator styleDecorator)
		{
			TextDecoration underLine = new TextDecoration();
			Pen p = CreateWpfPen(styleDecorator);
			underLine.Pen = p ;
			underLine.PenThicknessUnit = TextDecorationUnit.FontRecommended;
			textBlock.TextDecorations.Add(underLine);
		}

		#endregion

		Pen CreateWpfPen(TextStyleDecorator styleDecorator)
		{
			Pen myPen = new Pen();
			SolidColorBrush underlineBrush = brushConverter.ConvertFromString(styleDecorator.ForeColor.Name) as SolidColorBrush;
			myPen.Brush = underlineBrush;
			myPen.Thickness = 1.5;
			return myPen;
		}
		
		
		void SetDimension (FrameworkElement element,IBaseStyleDecorator decorator)
		{
			element.Width = decorator.DisplayRectangle.Width;
			element.Height = decorator.DisplayRectangle.Height;
//			element.MaxHeight = decorator.DisplayRectangle.Height;
//			element.MaxWidth = decorator.DisplayRectangle.Width;
		}
		
		
		public void ArrangePage(Size pageSize, FixedPage page)
		{
			page.Measure(pageSize);
			page.Arrange(new Rect(new System.Windows.Point(), pageSize));
			page.UpdateLayout();
		}
		
		
		public System.Windows.Size PageSize {get;set;}
	}
}
