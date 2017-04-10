using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Collections;

namespace iTextTokens
{
   
    partial class ReadTokens
    {
        public string procesare_sectiuni(string cale)
        {
            string yourpdfname = "20150211600"; 
            PdfReader reader = new PdfReader(cale + yourpdfname + ".PDF");
            TestDivider rc = new TestDivider();
            String content = rc.extractAndStore(reader, new StringBuilder(cale + yourpdfname + "_{0}_{1}.txt").ToString(), 319, 320);
            return content;
        }
    }

    class DividerAwareTextExtrationStrategy : LocationTextExtractionStrategy, IExtRenderListener
    {
        /// <summary>
        /// Clasa ce implementeaza LTES si interfata ITextChunkFilter si subclasa LTES.TextChunk pentru a prelua cuvinte si apoi interfaca IExtRenderListener pentru a prelua elementele grafice ce urmeaza sa detecteze dreptunghiuri de o anumita dimensiune si apoi se aplica si un filtru pentru culori
        /// </summary>

        float topMargin, bottomMargin, leftMargin, rightMargin;
        Vector moveToVector = null;
        Vector lineToVector = null;
       protected List<LineSegment> lines = new List<LineSegment>();
        public override void RenderText(TextRenderInfo renderInfo) //this is to get acces to LocationTES RenderText from the derived class = accesing/overriding a grandbase method in C#
        {
            base.RenderText(renderInfo);
        }

        //
        // constructor
        //
        /**
         * The constructor accepts top and bottom margin lines in user space y coordinates
         * and left and right margin lines in user space x coordinates.
         * Text outside those margin lines is ignored. 
         */
        public DividerAwareTextExtrationStrategy(float topMargin, float bottomMargin, float leftMargin, float rightMargin)
        {
            this.topMargin = topMargin;
            this.bottomMargin = bottomMargin;
            this.leftMargin = leftMargin;
            this.rightMargin = rightMargin;
        }

        //
        // Divider derived section support
        //
        public virtual List<Section> getSections()
        {
            List<Section> result = new List<Section>(); //this is the nested class Section and not Section from com.itextpdf.text from iText
            // TODO: Sort the array columnwise. In case of the OP's document, the lines already appear in the
            // correct order, so there was no need for sorting in the POC. 

            LineSegment previous = null;
            foreach (LineSegment line in lines)
            {
                if (previous == null)
                {
                    result.Add(new Section(null, line,this));
                }
                else if (Math.Abs(previous.GetStartPoint()[Vector.I1] - line.GetStartPoint()[Vector.I1]) < 2) // 2 is a magic number... 
                {
                    result.Add(new Section(previous, line, this));
                }
                else
                {
                    result.Add(new Section(previous, null, this));
                    result.Add(new Section(null, line, this));
                }
                previous = line;
            }
            return result;
        }

        public class Section : ITextChunkFilter
        {
            LineSegment topLine;
            LineSegment bottomLine;           
            float left, right, top, bottom;

            public Section(LineSegment topLine, LineSegment bottomLine, DividerAwareTextExtrationStrategy parent)
            {
                float left, right, top, bottom;
                if (topLine != null)
                {
                    this.topLine = topLine;
                    top = Math.Max(topLine.GetStartPoint()[Vector.I2], topLine.GetEndPoint()[Vector.I2]);
                    right = Math.Max(topLine.GetStartPoint()[Vector.I1], topLine.GetEndPoint()[Vector.I1]);
                    left = Math.Min(topLine.GetStartPoint()[Vector.I1], topLine.GetEndPoint()[Vector.I1]);
                }
                else
                {
                    top = parent.topMargin;
                    left = parent.leftMargin;
                    right = parent.rightMargin;
                }
                if (bottomLine != null)
                {
                    this.bottomLine = bottomLine;
                    bottom = Math.Min(bottomLine.GetStartPoint()[Vector.I2], bottomLine.GetEndPoint()[Vector.I2]);
                    right = Math.Max(bottomLine.GetStartPoint()[Vector.I1], bottomLine.GetEndPoint()[Vector.I1]);
                    left = Math.Min(bottomLine.GetStartPoint()[Vector.I1], bottomLine.GetEndPoint()[Vector.I1]);
                }
                else
                {
                    bottom = parent.bottomMargin;
                }
                this.top = top;
                this.bottom = bottom;
                this.left = left;
                this.right = right;
            }
            //
            // TextChunkFilter
            //
        public bool Accept(TextChunk textChunk)
            {
                // TODO: This code only checks the text chunk starting point. One should take the 
                // whole chunk into consideration
                Vector startlocation = textChunk.StartLocation;
                float x = startlocation[Vector.I1];
                float y = startlocation[Vector.I2];
                return (left <= x) && (x <= right) && (bottom <= y) && (y <= top);
            }
        }

            public void ClipPath(int rule)
        {
            throw new NotImplementedException();
        }

        public void ModifyPath(PathConstructionRenderInfo renderInfo)
        {
            switch (renderInfo.Operation)
            {
                case PathConstructionRenderInfo.MOVETO:
                    {
                        float x = renderInfo.SegmentData[0];
                        float y = renderInfo.SegmentData[1];
                        moveToVector = new Vector(x, y, 1);
                        lineToVector = null;
                        break;
                    }
                case PathConstructionRenderInfo.LINETO:
                    {
                        float x = renderInfo.SegmentData[0];
                        float y = renderInfo.SegmentData[1];
                        if (moveToVector != null)
                        {
                            lineToVector = new Vector(x, y, 1);
                        }
                        break;
                    }
                default:                    
                        moveToVector = null;
                        lineToVector = null;
                    break;
            }
        }

        public iTextSharp.text.pdf.parser.Path RenderPath(PathPaintingRenderInfo renderInfo)
        {
            if (moveToVector != null && lineToVector != null &&
            renderInfo.Operation != PathPaintingRenderInfo.NO_OP)
            {
                Vector from = moveToVector.Cross(renderInfo.Ctm);
                Vector to = lineToVector.Cross(renderInfo.Ctm);
                Vector extent = to.Subtract(from);
                if (Math.Abs(20 * extent[Vector.I2]) < Math.Abs(extent[Vector.I1]))
                {
                    LineSegment line;
                    if (extent[Vector.I1] >= 0)
                        line = new LineSegment(from, to);
                    else
                        line = new LineSegment(to, from);
                    lines.Add(line);
                }
            }
            moveToVector = null;
            lineToVector = null;
            return null;
        }
    }


    class DividerAndColorAwareTextExtractionStrategy : DividerAwareTextExtrationStrategy
    {
        BaseColor headerColor;
        //
        // constructor
        //
        public DividerAndColorAwareTextExtractionStrategy(float topMargin, float bottomMargin, float leftMargin, float rightMargin, BaseColor headerColor):base(topMargin, bottomMargin, leftMargin, rightMargin)
    {
            this.headerColor = headerColor;
        }

   

        public int compare(LineSegment o1, LineSegment o2)
        {
            Vector start1 = o1.GetStartPoint();
            Vector start2 = o2.GetStartPoint();
            float v1 = start1[Vector.I1], v2 = start2[Vector.I1];
            if (Math.Abs(v1 - v2) < 2)
            {
                v1 = start2[Vector.I2];
                v2 = start1[Vector.I2];
            }
            return v1.CompareTo(v2);
        }

        public override List<Section> getSections()
    {

            lines.Sort((x,y) => compare(x, y)); //sort lines using lamba method passed to delegate Comparison<T>
            //lines is from base class
        return base.getSections();
    }


    public override void RenderText(TextRenderInfo renderInfo)
        {
            if (approximates(renderInfo.GetFillColor(), headerColor))
            {
                lines.Add(renderInfo.GetAscentLine());
            }
            base.RenderText(renderInfo);
        }


        bool approximates(BaseColor colorA, BaseColor colorB)
        {
            if (colorA == null || colorB == null)
                return colorA == colorB;
            if ((colorA is CMYKColor) && (colorB is CMYKColor))
                {
                CMYKColor cmykA = (CMYKColor)colorA;
                CMYKColor cmykB = (CMYKColor)colorB;
                float c = Math.Abs(cmykA.Cyan - cmykB.Cyan);
                float m = Math.Abs(cmykA.Magenta - cmykB.Magenta);
                float y = Math.Abs(cmykA.Yellow - cmykB.Yellow);
                float k = Math.Abs(cmykA.Black - cmykB.Black);
                return c + m + y + k < 0.01;
            }
            // TODO: Implement comparison for other color types
            return false;
        }
   }


    class TestDivider
    {
        public TestDivider() { }

       public String extractAndStore(PdfReader reader, String format, int from, int to) 
        {
            StringBuilder builder = new StringBuilder();
    for (int page = from; page <= to; page++)
    {
        PdfReaderContentParser parser = new PdfReaderContentParser(reader);
        DividerAwareTextExtrationStrategy strategy = parser.ProcessContent(page, new DividerAwareTextExtrationStrategy(810, 30, 20, 575));
        List<DividerAwareTextExtrationStrategy.Section> sections = strategy.getSections(); //we can use simply keyword var but in iTextSharp library is a Section class in com.itextpdf.text
        int i = 0;
        foreach (DividerAwareTextExtrationStrategy.Section section in sections)
        {
            String sectionText = strategy.GetResultantText(section);
                 
                    string path = String.Format(format, page, i);                    
                    if (File.Exists(path))
                    {                       
                        File.Delete(path);
                    }
                    using (FileStream fs = File.Create(path))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(sectionText);
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                    }
                    builder.Append("--\n")
                   .Append(sectionText)
                   .Append('\n');
        i++;
        }
    builder.Append("\n\n");
    }

    return builder.ToString();
}

    }
}
