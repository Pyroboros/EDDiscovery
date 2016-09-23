﻿using Newtonsoft.Json.Linq;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EDDiscovery.EliteDangerous.JournalEvents
{
    public abstract class JournalLocOrJump : JournalEntry
    {
        public string StarSystem { get; set; }
        public Vector3 StarPos { get; set; }

        bool HasCoordinate { get { return !float.IsNaN(StarPos.X); } }

        protected JournalLocOrJump(JObject jo, JournalTypeEnum jtype, EDJournalReader reader) : base(jo, jtype, reader)
        {
            StarSystem = Tools.GetStringDef("StarSystem","Unknown!");

            Vector3 pos = new Vector3();

            if (Tools.IsNullOrEmptyT(jo["StarPos"]))            // if its an old VS entry, may not have co-ords
            {
                JArray coords = jo["StarPos"] as JArray;
                pos.X = coords[0].Value<float>();
                pos.Y = coords[1].Value<float>();
                pos.Z = coords[2].Value<float>();
            }
            else
            {
                pos.X = pos.Y = pos.Z = float.NaN;
            }

            StarPos = pos;
        }
    }
}
