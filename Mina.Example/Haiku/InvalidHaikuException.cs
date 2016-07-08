using System;

namespace Mina.Example.Haiku
{
    class InvalidHaikuException : Exception
    {
        public InvalidHaikuException(int position, string phrase,
                int syllableCount, int expectedSyllableCount)
            : base("phrase " + position + ", '" + phrase + "' had " + syllableCount
                        + " syllables, not " + expectedSyllableCount)
        {
            this.PhrasePositio = position;
            this.Phrase = phrase;
            this.SyllableCount = syllableCount;
            this.ExpectedSyllableCount = expectedSyllableCount;
        }

        public int ExpectedSyllableCount { get; }

        public string Phrase { get; }

        public int SyllableCount { get; }

        public int PhrasePositio { get; }
    }
}
