using System;
using System.IO;
// Importul librariilor necesare pentru iTextSharp 5.4.4
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using System.Collections.Generic;
using iTextTokens;
using iTextSharp.awt.geom; //pentru MyLinesV
using regex1;
using iTextSharp.tutorial.Chapter1;

namespace iTextSharp.tutorial.detectie_tabel
{
    public struct objdescrieri
    {
        internal List<List<int>> descrieri;
        internal List<List<StringBuilder>> cuvintepdf; //deoarece fiecare pagina are propriile cuvinte
        internal objdescrieri(List<List<int>> descrieri, List<List<StringBuilder>> cuvintepdf)
        {
            this.descrieri = descrieri;
            this.cuvintepdf = cuvintepdf;
        }
    }

    public class objtbnolines
    {
        public List<List<int>[]> matinit;
        public List<List<StringBuilder>> cuvinte; //deoarece fiecare pagina are propriile cuvinte
        public objtbnolines(List<List<int>[]> matinit, List<List<StringBuilder>> cuvinte)
        {
            this.matinit = matinit;
            this.cuvinte = cuvinte;
        }
    }

    partial class tabledetect
    {
        /// <summary>
        /// Extrage descrierea din pdf si converteste to word
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static objdescrieri pdftoword(string path)
        {
            var reader = new PdfReader(path);
            PdfReaderContentParser parser = new PdfReaderContentParser(reader);
            MyTES wordfinder = new MyTES();
   List<List<int>> descrieri = new List<List<int>>(); // contine descrierile din fiecare pagina
   List<List<StringBuilder>> cuvintepdf = new List<List<StringBuilder>>();
     for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                wordfinder = parser.ProcessContent(i, new MyTES());
                cuvintepdf.Add(wordfinder.cuvinte); // deoarece fiecare pagina are propriile cuvinte
                List<Line> medieneh = new List<Line>();
                List<Line> medienev = new List<Line>();
                foreach (Rectangle rect in wordfinder.dr_sir) //dr_sir reprezinta dreptunghiul ce incadreaza un string dintr-o coloana

                    if (rect != null)
                    {
                        medieneh.Add(new Line(rect.Left, rect.Bottom + rect.Top / 2, rect.Left + rect.Right, rect.Bottom + rect.Top / 2));
                        medienev.Add(new Line(rect.Left + rect.Right / 2, rect.Bottom, rect.Left + rect.Right / 2, rect.Bottom + rect.Top));

                    }

                ReadTokens rd1 = new ReadTokens();
                MyHLines<Line> liniih = new MyHLines<Line>();
                MyVLines<Line> liniiv = new MyVLines<Line>();
                string output = rd1.lines_approach(path, ref liniih, ref liniiv, i);
                //se aplica lines_approach pentru pagina la care suntem
                int length = wordfinder.cuvinte.Count;

                int[] vizitat = new int[length];//pentru fiecare element memorez daca a fost adaugat in vreo lista
                                                //completam de aici vecinii pe aceeasi linie
                for (int j = 0; j < length; j++) vizitat[j] = 0;
                
                //se adauga inca 2 vectori pentru coloana si vector , vom retine astfel coloana[tb.indice]
                int[] coloana = new int[length], linie = new int[length];

                List<int>[] matinit = new List<int>[0]; //e array unidimensional de lista de tabele spre deosebire de array-ul bidimensional de extragere celule si cuvintele din acele celule, acolo fiecare celula e un list
                for (int j = 0; j < length; j++)
                {
                    if (j<vizitat.Length && vizitat[j] == 0) //nu a fost adaugat in vreun tabel 
                    {
                        int ln = 0; bool gasit = false;
                        while (gasit == false)
                        {
                            if (matinit.Length > 0)
                            {
                                if (ln < matinit.Length && matinit[ln].Count >= 1)
                                {
                                    bool adaugat = false;
                                    List<int> list1 = matinit[ln];
                                    if (pe_aceeasi_linie(medieneh[j], medieneh[list1[0]]))
                                        if (matinit[ln].Contains(j))
                                            gasit = true;
                                        else
                                        {
                                            matinit[ln].Add(j); adaugat = gasit = true; break;
                                        }
                                    else ln++; //crestem linia
                                }
                                else //adaugam o noua linie in matinit deoarece am ajuns la sfarsit
                                {
                                    Array.Resize<List<int>>(ref matinit, matinit.Length + 1);
                                    matinit[matinit.Length - 1] = new List<int>();
                                    matinit[matinit.Length - 1].Add(j);
                                    gasit = true;
                                }

                            }
                            else //adaugam prima linie si primul element pe prima linie
                            {
                                Array.Resize<List<int>>(ref matinit, 1);
                                matinit[0] = new List<int>();
                                matinit[0].Add(j);
                                gasit = true;
                            }
                        }
                    }
                }

                //   afisare_linii(matinit, wordfinder.cuvinte);

              //  Console.WriteLine("=======================linii descriere===================");
                List<int> matdesc = extragere_descriere(matinit, ref vizitat, wordfinder.cuvinte, medieneh, wordfinder.listachar);
                descrieri.Add(matdesc); // contine descrierile din fiecare pagina in fiecare membru al listei
                //Console.WriteLine("pagina " + i.ToString());
            }

            reader.Close();
            return new objdescrieri(descrieri, cuvintepdf);
        }



        /// <summary>
        /// Extragere tabele fara linii
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static objtbnolines tbnolines(string path)
        {
            var reader = new PdfReader(path);
            PdfReaderContentParser parser = new PdfReaderContentParser(reader);     
            MyTES wordfinder = new MyTES();
            List<List<int>[]> matinitmare = new List<List<int>[]>();
            List<List<StringBuilder>> cuvintepdf = new List<List<StringBuilder>>();
            for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    wordfinder = parser.ProcessContent(i, new MyTES());
                cuvintepdf.Add(wordfinder.cuvinte);
                    List<Line> medieneh = new List<Line>();
                    List<Line> medienev = new List<Line>();
                    foreach (Rectangle rect in wordfinder.dr_sir) //dr_sir reprezinta dreptunghiul ce incadreaza un string dintr-o coloana

                        if (rect != null)
                        {
                            medieneh.Add(new Line(rect.Left, rect.Bottom + rect.Top / 2, rect.Left + rect.Right, rect.Bottom + rect.Top / 2));
                            medienev.Add(new Line(rect.Left + rect.Right / 2, rect.Bottom, rect.Left + rect.Right / 2, rect.Bottom + rect.Top));
                            
                        }

                    ReadTokens rd1 = new ReadTokens();
                    MyHLines<Line> liniih = new MyHLines<Line>();
                    MyVLines<Line> liniiv = new MyVLines<Line>();
                    string output = rd1.lines_approach(path, ref liniih, ref liniiv, i);
                //se aplica lines_approach pentru pagina la care suntem
                int length = wordfinder.cuvinte.Count;
                int[] vizitat = new int[length];//pentru fiecare element memorez daca a fost adaugat in vreo lista
                                                //completam de aici vecinii pe aceeasi linie
                for (int j = 0; j < length; j++) vizitat[j] = 0;
                //se adauga inca 2 vectori pentru coloana si vector , vom retine astfel coloana[tb.indice]
                int[] coloana = new int[length], linie = new int[length];

                List<int>[] matinit = new List<int>[0]; //e array unidimensional de lista de tabele spre deosebire de array-ul bidimensional de extragere celule si cuvintele din acele celule, acolo fiecare celula e un list
                for (int j = 0; j < length; j++)
                {
                    if (vizitat[j] == 0) //nu a fost adaugat in vreun tabel 
                    {
                        int ln = 0; bool gasit = false;
                        while (gasit == false)
                        {
                            if (matinit.Length > 0)
                            {
                                if (ln < matinit.Length && matinit[ln].Count >= 1)
                                {
                                    bool adaugat = false;
                                    List<int> list1 = matinit[ln];
                                    if (pe_aceeasi_linie(medieneh[j], medieneh[list1[0]]))
                                        if (matinit[ln].Contains(j))
                                            gasit = true;
                                        else
                                        {
                                            matinit[ln].Add(j); adaugat = gasit = true; break;
                                        }
                                    else ln++; //crestem linia
                                }
                                else //adaugam o noua linie in matinit deoarece am ajuns la sfarsit
                                {
                                    Array.Resize<List<int>>(ref matinit, matinit.Length + 1);
                                    matinit[matinit.Length - 1] = new List<int>();
                                    matinit[matinit.Length - 1].Add(j);
                                    gasit = true;
                                }

                            }
                            else //adaugam prima linie si primul element pe prima linie
                            {
                                Array.Resize<List<int>>(ref matinit, 1);
                                matinit[0] = new List<int>();
                                matinit[0].Add(j);
                                gasit = true;
                            }
                        }
                    }
                }

             //   afisare_linii(matinit, wordfinder.cuvinte);

            //    Console.WriteLine("=======================linii descriere===================");
                List<int> matdesc = extragere_descriere(matinit, ref vizitat, wordfinder.cuvinte, medieneh, wordfinder.listachar);
            
                for (int k=0; k<matinit.Length;k++)
                    matinit[k].RemoveAll(p => vizitat[p] > 0);
                List<int>[] matinit_tbnolines = new List<int>[0];
                for (int k = 0; k < matinit.Length; k++)
                    if (matinit[k].Count > 0)
                    {
                        Array.Resize<List<int>>(ref matinit_tbnolines, matinit_tbnolines.Length + 1);
                        matinit_tbnolines[matinit_tbnolines.Length - 1] = new List<int>(matinit[k]);

                    }
             //   Console.WriteLine("dupa remove vizitat:");
               // afisare_linii(matinit_tbnolines, wordfinder.cuvinte);

 
                matinitmare.Add(extragere_tabel_fara_linii(matinit_tbnolines, ref vizitat, medieneh, medienev));
 //afisare_linii(matinit3, wordfinder.cuvinte);

            //    Console.WriteLine("vectorul viz:");
              //  foreach (int r in vizitat) Console.Write(r + " ");
               
            }
            
            reader.Close();
            return new objtbnolines(matinitmare, cuvintepdf);
        }
    }
}
