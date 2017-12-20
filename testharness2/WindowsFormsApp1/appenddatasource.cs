﻿using System;
using work.bacome.imapclient;

namespace testharness2
{
    public abstract class cAppendDataSource
    {
        public static cAppendDataSource CurrentData = null;
    }

    public class cAppendDataSourceMessage : cAppendDataSource
    {
        public readonly cMessage Message;

        public cAppendDataSourceMessage(cMessage pMessage)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
        }

        public override string ToString() => Message.ToString(); 
    }

    public class cAppendDataSourceMessagePart : cAppendDataSource
    {
        public readonly cMessage Message;
        public readonly cSinglePartBody Part;

        public cAppendDataSourceMessagePart(cMessage pMessage, cSinglePartBody pPart)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            Part = pPart ?? throw new ArgumentNullException(nameof(pPart));
        }

        public override string ToString() => Message.ToString() + " " + Part.Section;
    }

    public class cAppendDataSourceString : cAppendDataSource
    {
        public readonly string String;

        public cAppendDataSourceString(string pString)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
        }

        public override string ToString() => String;
    }

    public class cAppendDataSourceFile : cAppendDataSource
    {
        public readonly string Path;

        public cAppendDataSourceFile(string pPath)
        {
            Path = pPath ?? throw new ArgumentNullException(nameof(pPath));
        }

        public override string ToString() => Path;
    }
}