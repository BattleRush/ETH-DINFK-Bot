using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSharpMath.SkiaSharp;
using SkiaSharp;

namespace ETHDINFKBot.Helpers
{
    public class Question
    {
        public string Subject { get; set; }
        public string Source { get; set; }
        public string Task { get; set; }
        public string Text { get; set; }
        public string Latex { get; set; }
        public string ExpectedInputFormat { get; set; }
        public string Hint { get; set; }
        public Stream Image { get; set; }
        public string Answer { get; set; }
        public string ExamQuestionImage { get; set; }
        public string SolutionImageString { get; set; }
        public Stream SolutionImage { get; set; }
    }


    public class StudyHelper
    {
        List<Question> LinAlgLatexQuestions = new List<Question>()
        {
            /*new Question()
            {
                Source = "FS20",
                Text = "Finden Sie die LR-Zerlegung **B = LR** der Matrix",
                ExpectedInputFormat = "The output is expected like this: L|R -> L11,L12,L13,L21,L22,L23,L31,L32,L33|R11,R12,R13,R21,R22,R23,R31,R32,R33",
                Hint = "Do the Gauss Algo. L is left under triangle matrix and R the uppter right one.",
                Latex = @"B = \begin{pmatrix} 1 &1  &0 \\ 2 &5  &-1 \\ 3 &6  &1 \end{pmatrix}",
                Answer = "12"
            },*/

            
            // 1
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "1_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :) ID: FS20_1_1",
                Latex = @"\text{Fur einen Untervektorraum X\subset V und eine lineare Abbildung \varphi : V → W ist das Bild \varphi (X)\subset W wieder ein Untervektorraum.}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "1_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :) ID: FS20_1_2",
                Latex = @"\text{Fur einen Untervektorraum X\subset W und eine lineare Abbildung \varphi : V → W ist das Urbild \\ \varphi^{−1} (X) = \{\, y \in V\, |\, \varphi (y) \in X\ \, \} \, \subset V wieder ein Untervektorraum.}",
                Answer = "true"
            },

            // 2
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "2_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei {\bf A}\in \mathbb{R}^{2x2} und {\bf b}\in \mathbb{R}^2. Wir nehmen an, dass keine Zeile von {\bf A} komplett 0 ist.} \\ \text{{\bf Ax=b} hat in jedem Fall eine Lösung.}",
                Answer = "false"
            },
            new Question()
            {Subject = "LinAlg",
                Source = "FS20",
                Task = "2_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei {\bf A}\in \mathbb{R}^{2x2} und {\bf b}\in \mathbb{R}^2. Wir nehmen an, dass keine Zeile von {\bf A} komplett 0 ist.} \\ \text{Die Losungsmenge von {\bf Ax=b} ist ein Pinkt in \mathbb{R}^2}",
                Answer = "false (Die Lösungsmenge könnte eine Gerade sein, oder nicht existieren.)"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "2_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei {\bf A}\in \mathbb{R}^{2x2} und {\bf b}\in \mathbb{R}^2. Wir nehmen an, dass keine Zeile von {\bf A} komplett 0 ist.} \\ \text{Geometrisch entspricht das Gleichungssystem dem Schneiden von zwei Geraden in 2D.}",
                Answer = "true"
            },


            // 3
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "3_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die orthogonalen Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ \text{Die Matrix {\bf A}^{T} ist orthogonal}",
                Answer = "true because A orthogonal -> A^T * A = I"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "3_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die orthogonalen Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ \text{Die Matrix {\bf A + B} ist orthogonal}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "3_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die orthogonalen Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ \text{Die Matrix {\bf A + A}^{T} ist orthogonal}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "3_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die orthogonalen Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ \text{Die Matrix {\bf AB}^{-1} ist orthogonal}",
                Answer = "true"
            },
            
            // 4
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "4_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei V ein endlich dimensionaler Vektorraum über einem Korper K und sei } {b_1,...,b_n} \text{ eine Basis von V.} \\
\text{Falls } n\geq 2 \text{ ist, dann sind die Vektoren }{b_1,...,b_n} \text{ paarweise linear unabhängig.}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "4_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei V ein endlich dimensionaler Vektorraum über einem Korper K und sei } {b_1,...,b_n} \text{ eine Basis von V.} \\ \text{ Falls }{c_1,...,c_m}\in V \text{ linear unabhängig sind, so gilt m\leq n}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "4_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei V ein endlich dimensionaler Vektorraum über einem Korper K und sei } {b_1,...,b_n} \text{ eine Basis von V.} \\ \text{Falls } c_1, ..., c_m\in V \text{ erzeugend sind, so gilt} m\leq n",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "4_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei V ein endlich dimensionaler Vektorraum über einem Korper K und sei } {b_1,...,b_n} \text{ eine Basis von V.} \\ \text{Falls } c_1, ..., c_m\in V \text{ eine Basis ist, so gilt} m = n",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "4_5",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei V ein endlich dimensionaler Vektorraum über einem Korper K und sei } {b_1,...,b_n} \text{ eine Basis von V.} \\ 
\text{Falls W ein weiterer Vektorraum und \varphi : V → W eine injektive lineare Abbildung ist, so ist {\varphi(b_1), ..., \varphi(b_n)} eine Basis von W.}",
                Answer = "false when dim(W) > dim(V)"
            },


            // 5
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "5_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Eine normierte Eigenzerlegung einer Matrix ist die Menge ihrer Eigenwerte mit zugehörigen normierten Eigenvektoren. Zwei Zerlegungen, die sich nur durch ihre Reihenfolgeunterscheiden, betrachten wir also identisch.} \\ 
\text{Eine Matrix {\bf A}\in \mathbb{R}^{nxn} hat eine eindeutig bestimmte normierte Eigenzerlegung.}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "5_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Eine normierte Eigenzerlegung einer Matrix ist die Menge ihrer Eigenwerte mit zugehörigen normierten Eigenvektoren. Zwei Zerlegungen, die sich nur durch ihre Reihenfolgeunterscheiden, betrachten wir also identisch.} \\ 
\text{Eine invertierbare Matrix {\bf A}\in \mathbb{R}^{nxn} hat eine eindeutig bestimmte normierte Eigenzerlegung.}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "5_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Eine normierte Eigenzerlegung einer Matrix ist die Menge ihrer Eigenwerte mit zugehörigen normierten Eigenvektoren. Zwei Zerlegungen, die sich nur durch ihre Reihenfolgeunterscheiden, betrachten wir also identisch.} \\ 
\text{Eine Matrix {\bf A}\in \mathbb{R}^{nxn} hat n verschiedene normierte Eigenzerlegungen.}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "5_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Eine normierte Eigenzerlegung einer Matrix ist die Menge ihrer Eigenwerte mit zugehörigen normierten Eigenvektoren. Zwei Zerlegungen, die sich nur durch ihre Reihenfolgeunterscheiden, betrachten wir also identisch.} \\ 
\text{Eine Matrix {\bf A}\in \mathbb{R}^{nxn} mit n verschiedenen Eigenwerten hat 2^n verschiedene normierte Eigenzerlegungen.}",
                Answer = "true"
            },

            // 6
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "6_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ 
\text{Es gilt det({\bf A})det({\bf B}) = det({\bf AB}).}",
                Answer = "true Satz 8.7"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "6_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ 
\text{Es gilt det({\bf A}) + det({\bf B}) = det ({\bf A + B}).}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "6_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ 
\text{Es gilt det({\bf A})^{-1} = det ({\bf A}^{-1}) als {\bf A} invertierbar ist.}",
                Answer = "true Korr. 8.8."
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "6_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Gegeben sind die Matrizen {\bf A}\in \mathbb{R}^{nxn} und {\bf B}\in \mathbb{R}^{nxn}.} \\ 
\text{Es gilt |det({\bf A})| = 1 genau dann wenn {\bf A} orthogonal ist.}",
                Answer = "false det = 1 is true for all orthogonal matrices but det = 1 doesnt mean it is one"
            },

            // 7
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "7_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Falls die Spalten der Matrizen {\bf B_1} und {\bf B_2} eine Basis des \mathbb{R}^{n} bilden, so gilt dies auch fur {\bf B_1B_2}.}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "7_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Jede Basis des \mathbb{R}^{n} hat genau n Elemente.}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "7_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Es gibt genau} n! \text{ Basen des \mathbb{R}^{n}.}",
                Answer = "false"
            },


            // 8
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "8_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Für zwei Matrizen {\bf A,B}\in \mathbb{R}^{2x2} gilt {\bf (A+B)^2 = A^2 + AB + BA + B^2}}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "8_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei {\bf A}\in \mathbb{R}^{nxn} gegeben und der Vector x\in \mathbb{R}^{m} unbekannt. Das System {\bf Ax = 0} hat genau dann eine eindeutig bestimmte Lösung, wenn die Zeilen von {\bf A} linear unabhängig sind.}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "8_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Sei {\bf A}\in \mathbb{R}^{nxn} gegeben und der Vector x\in \mathbb{R}^{m} unbekannt. Das System {\bf Ax = 0} hat genau dann eine eindeutig bestimmte Lösung, wenn die Spalten von {\bf A} linear unabhängig sind.}",
                Answer = "true"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "8_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Zeilenoperationen einer Matrix {\bf A} ̈andern im Allgemeinen die Lösbarkeit des Gleichungssystems {\bf Ax = b}.}",
                Answer = "false"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "FS20",
                Task = "8_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Latex = @"\text{Wenn die Matrix {\bf A} Zeilenstufenform hat, so ist der Rang der Matrix gleich der Anzahl der nicht verschwindenden (komplett aus Nullen bestehenden) Spalten.}",
                Answer = "false"
            },
            

            //// HS 19
            // 1
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "1_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_1_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "1_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_1_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "1_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_1_3.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "1_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_1_4.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "1_5",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_1_5.png"
            },


            // 2
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "2_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_2_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "2_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_2_2.png"
            },


            // 3
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "3_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_3_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "3_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false (can also be a plane)",
                ExamQuestionImage = "HS19_3_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "3_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_3_3.png"
            },



            // 4
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "4_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_4_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "4_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_4_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "4_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_4_3.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "4_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_4_4.png"
            },

            // 5
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "5_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_5_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "5_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_5_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "5_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_5_3.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "5_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_5_4.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "5_5",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_5_5.png"
            },


            // 6
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "6_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_6_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "6_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_6_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "6_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_6_3.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "6_4",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_6_4.png"
            },

            // 7
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "7_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "Correct are 6 (1x2x3) and 35 (1x5x7)",
                ExamQuestionImage = "HS19_7_1.png"
            },

            // 8
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "8_1",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_8_1.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "8_2",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "true",
                ExamQuestionImage = "HS19_8_2.png"
            },
            new Question()
            {
                Subject = "LinAlg",
                Source = "HS19",
                Task = "8_3",
                Text = "Solve the Multiple-Choice question",
                ExpectedInputFormat = "1 or 0 for (true or false)",
                Hint = "None (if you have one please send one to BattleRush :)",
                Answer = "false",
                ExamQuestionImage = "HS19_8_3.png"
            }

            
            
   




            // AND
                    
            
          /* 
            new Question()
            {
                Subject = "AnD",
                Source = "PVW",
                Task = "Custom_1",
                Text = "find an order for which it holds that if a function f is to the left of g, then g grows strictly faster than f",
                Hint = "",
                Answer = "false",
                ExamQuestionImage = "Custom_1_1.png",
                SolutionImageString = "Solution_Custom_1_1.png"
            },


            new Question()
            {
                Subject = "AnD",
                Source = "PVW",
                Task = "Task 1_1",
                Text = "True or false",
                Latex = @"\log_2 \left(n^{1000}\right) \in O\left(\log_{10} \sqrt{n}\right)",
                Hint = "",
                Answer = "false",
            }

            */
        };

        public int GetQuestionCount()
        {
            return LinAlgLatexQuestions.Count;
        }
        public string GetExams()
        {
            return string.Join(", ", LinAlgLatexQuestions.Select(i => i.Subject).Distinct());
        }

        public Question GetRandomLinalgQuestion(string filter = null)
        {
            return null;/*

            Random r = new Random();

            var questions = LinAlgLatexQuestions;

            if (!string.IsNullOrWhiteSpace(filter))
            {
                questions = questions.Where(i => i.Source.ToLower() == filter.ToLower()).ToList();
            }

            var question = questions[r.Next(0, questions.Count)];

            if (!string.IsNullOrWhiteSpace(question.Latex) && string.IsNullOrWhiteSpace(question.ExamQuestionImage))
            {
                // \color{{white}}
                var painter = new MathPainter
                {
                    LaTeX = $@"{ question.Latex}",
                    //TextColor = SKColor.Parse("FFFFFF"),
                    TextColor = SKColor.Parse("000000"),
                    AntiAlias = true

                }; // or TextPainter
                painter.FontSize = 25;
                var png = painter.DrawAsStream();


                int padding = 15;

                Bitmap src = new Bitmap(png);
                Bitmap target = new Bitmap(src.Size.Width + 2 * padding, src.Size.Height + 2 * padding);
                Graphics g = Graphics.FromImage(target);
                //g.Clear(Color.FromArgb(54, 57, 63));
                g.Clear(Color.FromArgb(255, 255, 255));
                //g.FillRectangle(new SolidBrush(Color.Red), 0, 0, target.Width + 2 * padding, target.Height + 2 * padding);
                g.DrawImage(src, padding, padding);

                Stream ms = new MemoryStream();
                target.Save(ms, ImageFormat.Png);
                ms.Position = 0;



                question.Image = ms;
            }
            else if (!string.IsNullOrWhiteSpace(question.ExamQuestionImage))
            {
                try
                {
                    var pathToImage = Path.Combine(Program.BasePath, "ExamQuestions", question.ExamQuestionImage);
                    Console.WriteLine(pathToImage);
                    var file = new FileStream(pathToImage, FileMode.Open);

                    question.Image = file;
                }
                catch (Exception ex)
                {
                    question.Text += " " + ex.Message;
                }

            }

            if (!string.IsNullOrWhiteSpace(question.SolutionImageString))
            {
                try
                {
                    var pathToImage = Path.Combine(Program.BasePath, "ExamQuestions", question.SolutionImageString);
                    Console.WriteLine(pathToImage);
                    var file = new FileStream(pathToImage, FileMode.Open);

                    question.SolutionImage = file;
                }
                catch (Exception ex)
                {
                    question.Text += " " + ex.Message;
                }

            }


            return question;*/
        }

    }
}
