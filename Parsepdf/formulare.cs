using System;
using System.IO;
// Importul librariilor necesare pentru iTextSharp 5.4.4
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using iTextTokens;
using iTextSharp.awt.geom; //pentru MyLinesV
using regex1;
using iTextSharp.tutorial.Chapter1;

namespace iTextSharp.tutorial.detectie_tabel
{
    struct tipform
    {
      internal int tip;
      internal  float poz;
        internal string text;
        internal int index_ales; // pentru controale de tip radio
       internal tipform(int tip, float poz, string text, int index_ales)
        {
            this.tip = tip;
            this.poz = poz;
            this.text = text;
            this.index_ales = index_ales;
        }
    }
    partial class tabledetect
    {
        /// <summary>
        /// functia de extragere formulare finala care o apeleaza si pe cea de jos
        /// </summary>
        /// <param name="path"></param>
        public static List<List<string>> formfinal(string path)
        {
            var reader = new PdfReader(path);
            PdfReaderContentParser parser = new PdfReaderContentParser(reader);
            MyTES wordfinder = new MyTES();
            List<List<string>> afisareformulare = new List<List<string>>();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                wordfinder = parser.ProcessContent(i, new MyTES());
                AcroFields fields = reader.AcroFields; //pastram si acrofields
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
                //aici se extrag formularele
afisareformulare.Add(extragere_formulare(matinit, wordfinder.cuvinte, medieneh, fields));
            }
            reader.Close();
            return afisareformulare;
        }

        public static List<string> extragere_formulare(List<int>[] mat, List<StringBuilder> cuvinte, List<Line> medieneh, AcroFields fields)
        {
            //mat = lista liniilor din pdf
            //e posibil ca formularul sa fie dupa niste coloane sau tabele, oricum se acopera si o parte din aceste cazuri deoarece vin pe linii si iau cuvintele consecutive
            //trebuie ca formularul sa fie pe o anumita lungime si sa mai aiba caracteristici precum caractere pentru cuvinte lipsa,linii, si acroforms, e posibil ca un formular sa contina punctele a si b pe prima linie si separate ca si cum ar fi pe 2 coloane
            //e posibil sa avem, ca in cgt12.pdf, un rand 8. a. -- daca se ia doar inceputul randului adica doar 8., textul din rand nu va fi luat, trebuie verificat  daca textul se continua cu un alt inceput , se pot lua separat, de exemplu in Part A, se ia Part si apoi A in urmatorul element - ne oprim la ultimul delimitator de dupa textul de maxim 2 caractere. 
            List<int> tbl = new List<int>();
            List<string> elemente = new List<string>(); //elementele pe care le adaugam in sir pentru patternstrings2
            int[] tati = new int[0]; //indicii din tati sunt legati de elemente
            List<int> indiceelemente = new List<int>(); //adaugam stringuri in elemente dar trebuie pastrati si indicii cuvintelor == indici din sirul de cuvinte(wordfinder.cuvinte), 
            List<float> linieelement = new List<float>(); //linia pe care se afla fiecare element, pastram y-ul pentru a gasi cel mai apropiat acroform
            //dar daca sunt mai multe acroforms ca-n CGT12.pdf, nu este indeajuns y - ba este indeajuns pentru ca si chiar daca sunt pe 2 coloane(ceea ce se intampla rar), luam acroformul care apare primul si problema ar trb sa fie rezolvata deoarece textul vine succesiv pe cele 2 coloane

            //parcurgem matricea de linii si adaugam inceputul de cuvant al liniei 
            //se merge pe linie si ce e consecutiv este marcat ca fiind parte dintr-o linie se ia inceputul primului cuvant sau daca
            List<List<int>> vizmat = new List<List<int>>(); //ne spune care valori din matricea de linii a fost vizitat 

            //nr_elemente va contine numarul de elemente din matricea de linii
            int nr_elemente = 0;
            for (int i = 0; i < mat.Length; i++)
                nr_elemente += mat[i].Count;

            //construim matricea de vizitat care e de fapt matricea de coloane 
            for (int i = 0; i < mat.Length; i++)
            {
                //prima coloana este marcata cu 1, a2a cu 2 si tot asa
                vizmat.Add(new List<int>());
                for (int q = 0; q < mat[i].Count; q++)
                    vizmat[i].Add(-1);
            }

            /*
            //folosim o matrice de stringuri - pas de preprocesare
            List<List<string>> worduri = new List<List<string>>();//separam matricea in cuvinte
            for (int i = 0; i < mat.Length; i++)
            {
                List<int> linie = mat[i];
                worduri.Add(new List<string>());
                foreach (int k in linie)
                {
                    string[] sir = cuvinte[k].ToString().Split(' ');
                    foreach (string s in sir)
                        //le adaugam pe toate in lista de stringuri numita "worduri"
                        worduri[i].Add(s);
                }
            }
            */
            int elemente_adaugate = 0, vizitate = 0;
            while (vizitate < nr_elemente)
            {
                for (int i = 0; i < mat.Length; i++)
                {
                    List<int> linie = vizmat[i];//luam linia din matricea corespondenta de elemente vizitate    
                    int k = 0;
                    //punem inceputurile(prin split pe delimitatorul spatiu) in Lista linie, daca am mai gasit ceva asemanator revenim la linia unde eram si mai punem in continuare.
                    for (int w = 0; w < linie.Count; w++) // ne oprim pe primul nevizitat, adica pe -1
                    {
                        if (linie[w] == -1) { k = w; break; }
                    }
                    string[] sir = cuvinte[mat[i][k]].ToString().Split(' ');
                    if (sir.Length == 1) elemente.Add(sir[0]); //pentru Part A
                    else
                        elemente.Add(sir[0] + " " + sir[1]);
                    elemente_adaugate++; vizitate++;
                    Array.Resize(ref tati, tati.Length + 1); tati[tati.Length - 1] = -1;
                   // regex.patternstrings_nou(elemente, ref tati); //se apeleaza la fiecare nou string adaugat, se poate modifica pentru optimizare astfel incat patternul sa fie construit cand avem tot sirul
                    vizmat[i][k] = 1;
                    // indiceelemente.Add(mat[i][k]);//contorul pentru elemente dar si pentru indiceelemente este elemente_adaugate
                   // if (k>0)
                        indiceelemente.Add(i);
                    //pentru afisare formular cu acroforms, adaugam indicele i din mat deoarece afisam toata linia mat[i] deoarece in mat[i] se ia un element a) si un alt element ce urmeaza
                   // if (k > 0) // ??? nu adaugam doar daca k > 0
                        linieelement.Add(gety(medieneh[mat[i][0]]));
                    //continuam marcarea vecinilor de pe aceeasi linie //poate ar trebui folosita si o functie distanta, sa nu fie distanta prea mare intre cuvinte
                    //se poate merge pana exista separare cu linii intre cuvinte 
                    //o situatie precum CGT12 in care sunt 2 formulare pe 2 coloane este destul de rara
                    List<int> liniemat = mat[i];
                    if (k < liniemat.Count - 1)
                        for (int q = k + 1; q < liniemat.Count; q++)
                            if (Math.Abs(liniemat[q] - liniemat[q - 1]) <= 2) //de ce mai trebuie verificat asta din moment ce sunt pe aceeasi linie in mat ?
                            {
                                vizmat[i][q] = 1; vizitate++;
                                //indicii parcursi aici de q nu sunt adaugati in lista de elemente deci nu e nevoie de update si la celelalte structuri
                                //problema aici este ca trebuie sa creasca nr elemente astfel incat sa se oprim while-ul
                            }
                            else break; //in momentul in care progresia nu se mai verifica, ne oprim

                }
            }
            //mai trebuie initializata si linia si cuvintele
            //initializare tati(dupa ce am creat elemente) si apelare pattern2
            //for (int t = 0; t < elemente_adaugate; t++)
            //{
            //    Array.Resize(ref tati, tati.Length + 1);
            //    tati[tati.Length - 1] = -1;
            //}
      
            //afisare tati
            //      Console.WriteLine("Get Y");
            //for (int t = 0; t < elemente_adaugate; t++)
              //  Console.WriteLine(tati[t] + " " + elemente[t] + " " + linieelement[t]);
            //Console.WriteLine();

 regex.patternstrings_stiva(ref elemente, ref tati, ref linieelement, ref indiceelemente); //pentru stiva se apeleaza la sfarsit pentru toate elementele
            //regex.afisare_pattern_nou(elemente, tati);


            List<tipform> listform= new List<tipform>();
            //acum introducem si acroforms 
            List<string> fldNames = new List<string>(fields.Fields.Keys);

         
            if (fldNames.Count > 0) //am gasit cel putin un acroform
            {
                foreach (string fldname in fldNames) //toata aceasta structura se poate cu LINQ imbricat
                {
                    int tip = fields.GetFieldType(fldname);
                    string txt = fields.GetField(fldname);
                   // Console.WriteLine(fldname + " " + tip + " " + txt + " la pozitia");
                    List<AcroFields.FieldPosition> locatii = new List<AcroFields.FieldPosition>(fields.GetFieldPositions(fldname));
                    int index = 0;
                    if (tip == 3) //choice = radio
                    {
                     //   Console.WriteLine("alegeri radio");
                        string[] valori = fields.GetListSelection(fldname);
                      //  foreach (string s in valori)
                        //    Console.WriteLine(s + " ");
                        //Console.WriteLine("selected from radio form options");
                        string[] valori2 = fields.GetAppearanceStates(fldname);
                        ///Console.WriteLine(valori2.Length);
                        var val2 = (from string c in valori2
                                   where (c.ToLower().CompareTo("off") != 0)
                                   select c).ToList();
                        if (val2.Count > 0)
                        {
                            int i2 = 0;
                            foreach (string s2 in val2)
                            {
                                i2++;
                              //  Console.WriteLine(s2 + " ");
                                if (s2.ToLower().CompareTo(valori[0].ToLower()) == 0) index = i2;
                            }
                          //  listform[listform.Count - 1] = new tipform(listform[listform.Count - 1].tip, listform[listform.Count - 1].poz, listform[listform.Count - 1].text, index);
                          //  Console.WriteLine("la pozitia {0}", index);
                        }

    
                   

                        // var index = valori2.Where((r, i) => r.ToLower().CompareTo(valori[0]) != 0).Select(new { });


                        // AcroFields.Item radio = fields.GetFieldItem(fldname);
                        //for (int q = 0; q < locatii.Count; q++) ;
                        //  Console.WriteLine(radio.GetValue(q ).Keys[0]+"-");
                    }
                    foreach (AcroFields.FieldPosition q in locatii) //l-am mutat mai jos deoarece mai intai trebuie fixat index pt radio
                    {
                       // Console.WriteLine(q.position.Left + " " + (q.position.Bottom + (q.position.Height / 2)));
                        listform.Add(new tipform(tip, q.position.Bottom + (q.position.Height / 2), txt, index));
                    }
                }
            }
            //indicelement ne da indicele i al lui mat[i]
            //linieelement ne da coord y a lui mat[i]
            int[] vizacro = new int[listform.Count];
            int[] vizlinie = new int[indiceelemente.Count];
            int[] coordy;
            for (int q = 0; q < vizacro.Length; q++) vizacro[q] = -1;
            for (int q = 0; q < indiceelemente.Count; q++) vizlinie[q] = -1;
            //parcurgem indiceelemente si afisam linia plus control box 
            //cuplam indiceelemente(pozitia top a cuvintelor cu pozitia top a formelor)
            Dictionary<int, int> perechi = new Dictionary<int, int>();
           
                for (int t = 0; t < listform.Count; t++)
                {
                float min = Int32.MaxValue;
                int indicelinie = 0, pozitieacro = 0; ; bool gasit = false;
                for (int q = 0; q < indiceelemente.Count; q++)
                        if (t < vizacro.Length && indiceelemente[q] < linieelement.Count && q < vizlinie.Length && vizacro[t] == -1 && vizlinie[q] == -1 && Math.Abs(listform[t].poz - linieelement[indiceelemente[q]]) < min)
                        {
                        min = Math.Abs(listform[t].poz - linieelement[indiceelemente[q]]);
                        indicelinie = q; // a cata linie a listei indiceelemente, pentru ca asta cuplam indiceelemente cu listform fiecare de la 0 la ce nr de elemente are
                        pozitieacro = t;//pentru vizitat
                        if (min < 50) gasit = true; //euristic stabilit
                    }

                if (gasit)
                {
                    perechi.Add(indicelinie, pozitieacro);
                    vizacro[pozitieacro]++;
                    vizlinie[indicelinie]++;
                }
                
            }

            //afisare c form
         //   Console.WriteLine("\n\nafisare form\n");
            List<string> afisare = new List<string>(); 
            //lista aceasta se va transmite functiei word de creare a fisierului
            //se adauga un nou element doar daca se trece pe o noua linie
            //am ales aceasta varianta deoarece in word conteaza doar textul 
            int ord_radio = 0;
            for (int q = 0; q < indiceelemente.Count; q++)
            {
                string s = String.Empty ;
                if (perechi.Keys.Contains(q) && listform[perechi[q]].tip == 3) //am intalnit un radio buton 
                {
                    ord_radio++;
                 
                    if (ord_radio == listform[perechi[q]].index_ales)
                    {
                        s = String.Empty;
                        for (int r = 0; r < mat[indiceelemente[q]].Count; r++)
                        {
                            // Console.Write(cuvinte[mat[indiceelemente[q]][r]] + " ");
                            s += cuvinte[mat[indiceelemente[q]][r]] + " ";
                        }
                        afisare.Add(s);
                        //Console.WriteLine();
                        continue;
                    }
                    else continue; //nu se afiseaza celelalte randuri ce corespund celorlalte optiuni nealese radio
                }
               
                    ord_radio = 0; //fara else deoarece daca vin 2 formulare radio ord_radio va creste in continuare, asa la primul rand titlu devine 0

                s = String.Empty;
                for (int r = 0; r < mat[indiceelemente[q]].Count; r++)
                {
                    //Console.Write(cuvinte[mat[indiceelemente[q]][r]] + " ");
                    s += cuvinte[mat[indiceelemente[q]][r]] + " ";
                }
                if (perechi.Keys.Contains(q))
                    // Console.Write("-"+listform[perechi[q]].text);
                    s += "-" + listform[perechi[q]].text;
                //  Console.WriteLine();
                afisare.Add(s);
            }

            //acum afisam lista de stringuri
            //foreach (string s in afisare)
              //  Console.WriteLine(s);
            return afisare;
        }

    }
}
