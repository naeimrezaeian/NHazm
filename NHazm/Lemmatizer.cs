﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NHazm
{
    public class Lemmatizer
    {
        private Hashtable _verbs;
        private HashSet<string> _words;

        public Lemmatizer() : this("data/words.dat", "data/verbs.dat", true) { }
        public Lemmatizer(bool joinedVerbParts) : this("data/words.dat", "data/verbs.dat", joinedVerbParts) { }
        public Lemmatizer(string wordsFile, string verbsFile) : this(wordsFile, verbsFile, true) { }
        public Lemmatizer(string wordsFile, string verbsFile, bool joinedVerbParts)
        {
            Stemmer stemmer = new Stemmer();

            this._words = new HashSet<string>();
            foreach (var line in File.ReadLines(wordsFile))
                this._words.Add(line.Trim());

            WordTokenizer tokenizer = new WordTokenizer(verbsFile);

            var pureVerbs = new List<string>(File.ReadAllLines(verbsFile).Reverse());

            this._verbs = new Hashtable();
            this._verbs.Add("است", "#است");
            pureVerbs.ForEach(verb =>
            {
                Conjugations(verb).ForEach(tense =>
                {
                    if (!this._verbs.ContainsKey(tense))
                        this._verbs.Add(tense, verb);
                });
            });

            if (joinedVerbParts)
            {
                pureVerbs.ForEach(verb =>
                {
                    string[] parts = verb.Split('#');
                    tokenizer.AfterVerbs.ToList().ForEach(afterVerb =>
                    {
                        this._verbs.Add(parts[0] + "ه " + afterVerb, verb);
                    });
                    tokenizer.BeforeVerbs.ToList().ForEach(beforeVerb =>
                    {
                        this._verbs.Add(beforeVerb + " " + parts[0], verb);
                    });
                });
            }
        }

        public string Lemmatize(string word)
        {
            return Lemmatize(word, "");
        }
        public string Lemmatize(string word, string pos)
        {
            if ((pos.Length == 0 || pos.Equals("V")) && this._verbs.ContainsKey(word))
                return this._verbs[word].ToString();

            if (this._words.Contains(word))
                return word;

            var stem = new Stemmer().Stem(word);
            if (this._words.Contains(stem))
                return stem;

            return word;
        }

        public List<string> Conjugations(string verb)
        {
            List<string> ends = new List<string>(new string[] { "م", "ی", "", "یم", "ید", "ند" });

            if (verb.Equals("#هست"))
            {
                List<string> conjugate1 = new List<string>();
                List<string> conjugate2 = new List<string>();
                ends.ForEach(end =>
                {
                    conjugate1.Add("هست" + end);
                    conjugate2.Add("نیست" + end);
                });
                return conjugate1.Concat(conjugate2).ToList();
            }

            HashSet<string> conjugates = new HashSet<string>();
            string[] parts = verb.Split(new char[] { '#' }, StringSplitOptions.None);
            string past = parts[0];
            string present = parts[1];

            ends.ForEach(end =>
            {
                string conj = past + end;
                string nconj;

                // pastSimples
                conj = GetRefinement(conj);
                conjugates.Add(conj);
                nconj = GetRefinement(GetNot(conj));
                conjugates.Add(nconj);


                conj = "می‌" + conj;

                // pastImperfects
                conj = GetRefinement(conj);
                conjugates.Add(conj);
                nconj = GetRefinement(GetNot(conj));
                conjugates.Add(nconj);
            });

            ends = new List<string>(new string[] { "ه‌ام", "ه‌ای", "ه", "ه‌ایم", "ه‌اید", "ه‌اند" });

            // pastNarratives
            ends.ForEach(end =>
            {
                string conj = past + end;
                conjugates.Add(GetRefinement(conj));
                conjugates.Add(GetRefinement(GetNot(conj)));
            });

            conjugates.Add(GetRefinement("ب" + present));
            conjugates.Add(GetRefinement("ن" + present));

            if (present.EndsWith("ا") || new string[] { "آ", "گو" }.Contains(present))
                present = present + "ی";

            ends = new List<string>(new string[] { "م", "ی", "د", "یم", "ید", "ند" });

            List<string> presentSimples = new List<string>();
            ends.ForEach(end =>
            {
                string conj = present + end;
                presentSimples.Add(conj);

                conjugates.Add(GetRefinement(conj));
                conjugates.Add(GetRefinement(GetNot(conj)));
            });

            presentSimples.ForEach(item =>
            {
                string conj;

                // presentImperfects
                conj = "می‌" + item;
                conjugates.Add(GetRefinement(conj));
                conjugates.Add(GetRefinement(GetNot(conj)));

                // presentSubjunctives
                conj = "ب" + item;
                conjugates.Add(GetRefinement(conj));

                // presentNotSubjunctives
                conj = "ن" + item;
                conjugates.Add(GetRefinement(conj));
            });

            return conjugates.ToList();
        }

        private string GetRefinement(string text)
        {
            return text.Replace("بآ", "بیا").Replace("نآ", "نیا");
        }

        private string GetNot(string text)
        {
            return "ن" + text;
        }
    }
}