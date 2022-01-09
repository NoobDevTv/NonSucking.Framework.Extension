namespace DEMO
{
    public partial class SUTMessage
    {
        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Positions.Length);
            foreach (var item__Positions__s in Positions)
            {
                writer.Write(item__Positions__s.X);
                writer.Write(item__Positions__s.Y);
            }

            writer.Write(Type);
            writer.Write(Text);
            writer.Write(UsersList.Count);
            foreach (var item__UsersList__t in UsersList)
            {
                item__UsersList__t.SerializeMe(writer);
            }

            writer.Write(ComplainsBases.Count);
            foreach (var item__ComplainsBases__u in ComplainsBases)
            {
                writer.Write(item__ComplainsBases__u.Complain);
                writer.Write(item__ComplainsBases__u.Complains.Count);
                foreach (var item__Complains__v in item__ComplainsBases__u.Complains)
                {
                    writer.Write(item__Complains__v);
                }
            }

            writer.Write(Countings.Count);
            foreach (var item__Countings__w in Countings)
            {
                writer.Write(item__Countings__w);
            }

            writer.Write(CountingDic.Count);
            foreach (var item__CountingDic__x in CountingDic)
            {
                writer.Write(item__CountingDic__x.Key);
                writer.Write(item__CountingDic__x.Value.Complain);
                writer.Write(item__CountingDic__x.Value.Complains.Count);
                foreach (var item__Complains__y in item__CountingDic__x.Value.Complains)
                {
                    writer.Write(item__Complains__y);
                }
            }

            writer.Write(ReadOnlyCountings.Count);
            foreach (var item__ReadOnlyCountings__z in ReadOnlyCountings)
            {
                writer.Write(item__ReadOnlyCountings__z);
            }

            writer.Write(ReadOnlyCountingsButSetable.Count);
            foreach (var item__ReadOnlyCountingsButSetable__A in ReadOnlyCountingsButSetable)
            {
                writer.Write(item__ReadOnlyCountingsButSetable__A);
            }

            writer.Write(ReadOnlyDicSetable.Count);
            foreach (var item__ReadOnlyDicSetable__B in ReadOnlyDicSetable)
            {
                writer.Write(item__ReadOnlyDicSetable__B.Key);
                writer.Write(item__ReadOnlyDicSetable__B.Value);
            }

            writer.Write((int)Right);
            writer.Write(Complain.Complain);
            writer.Write(Complain.Complains.Count);
            foreach (var item__Complains__C in Complain.Complains)
            {
                writer.Write(item__Complains__C);
            }

            AssignedUser.SerializeMe(writer);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            DEMO.SUTMessage.User.SerializeMe(writer, ContactUser);
            DEMO.SUTMessage.User.SerializeMe(writer, AlternativUser);
            writer.Write(X);
            writer.Write(countPositions);
            writer.Write(randomField);
            writer.Write(forCtor);
        }

        public static SUTMessage Deserialize(System.IO.BinaryReader reader)
        {
            var count__Positions__J = reader.ReadInt32();
            var Positions__ret__I = new System.Collections.Generic.List<System.Drawing.Point>(count__Positions__J);
            for (int i____K = 0; i____K < count__Positions__J; i____K++)
            {
                System.Drawing.Point Point____H;
                var X__Point__F = reader.ReadInt32();
                var Y__Point__G = reader.ReadInt32();
                Point____H = new System.Drawing.Point(X__Point__F, Y__Point__G);
                Positions__ret__I.Add(Point____H);
            }

            var Type__ret__L = reader.ReadInt32();
            var Text__ret__M = reader.ReadString();
            var count__UsersList__Q = reader.ReadInt32();
            var UsersList__ret__P = new System.Collections.Generic.List<DEMO.SUTMessage.User>(count__UsersList__Q);
            for (int i____R = 0; i____R < count__UsersList__Q; i____R++)
            {
                var User____O = DEMO.SUTMessage.User.DeserializeMe(reader);
                UsersList__ret__P.Add(User____O);
            }

            var count__ComplainsBases__bc = reader.ReadInt32();
            var ComplainsBases__ret__bb = new System.Collections.Generic.List<DEMO.ComplainBase>(count__ComplainsBases__bc);
            for (int i____bd = 0; i____bd < count__ComplainsBases__bc; i____bd++)
            {
                DEMO.ComplainBase ComplainBase____ba;
                var Complain__ComplainBase__U = reader.ReadString();
                var count__Complains__Y = reader.ReadInt32();
                var Complains__ComplainBase__X = new System.Collections.Generic.List<string>(count__Complains__Y);
                for (int i____Z = 0; i____Z < count__Complains__Y; i____Z++)
                {
                    var String____W = reader.ReadString();
                    Complains__ComplainBase__X.Add(String____W);
                }

                ComplainBase____ba = new DEMO.ComplainBase();
                ComplainBase____ba.Complain = Complain__ComplainBase__U;
                ComplainBase____ba.Complains = Complains__ComplainBase__X;
                ComplainsBases__ret__bb.Add(ComplainBase____ba);
            }

            var count__Countings__bh = reader.ReadInt32();
            var Countings__ret__bg = new System.Collections.Generic.List<short>(count__Countings__bh);
            for (int i____bi = 0; i____bi < count__Countings__bh; i____bi++)
            {
                var Int16____bf = reader.ReadInt16();
                Countings__ret__bg.Add(Int16____bf);
            }

            var count__CountingDic__bt = reader.ReadInt32();
            var CountingDic__ret__bs = new System.Collections.Generic.Dictionary<short, DEMO.ComplainBase>(count__CountingDic__bt);
            for (int i____bu = 0; i____bu < count__CountingDic__bt; i____bu++)
            {
                var key__CountingDic__bj = reader.ReadInt16();
                DEMO.ComplainBase value__CountingDic__bk;
                var Complain__value__CountingDic__bk__bm = reader.ReadString();
                var count__Complains__bq = reader.ReadInt32();
                var Complains__value__CountingDic__bk__bp = new System.Collections.Generic.List<string>(count__Complains__bq);
                for (int i____br = 0; i____br < count__Complains__bq; i____br++)
                {
                    var String____bo = reader.ReadString();
                    Complains__value__CountingDic__bk__bp.Add(String____bo);
                }

                value__CountingDic__bk = new DEMO.ComplainBase();
                value__CountingDic__bk.Complain = Complain__value__CountingDic__bk__bm;
                value__CountingDic__bk.Complains = Complains__value__CountingDic__bk__bp;
                CountingDic__ret__bs.Add(key__CountingDic__bj, value__CountingDic__bk);
            }

            var count__ReadOnlyCountings__by = reader.ReadInt32();
            var ReadOnlyCountings__ret__bx = new System.Collections.Generic.List<short>(count__ReadOnlyCountings__by);
            for (int i____bz = 0; i____bz < count__ReadOnlyCountings__by; i____bz++)
            {
                var Int16____bw = reader.ReadInt16();
                ReadOnlyCountings__ret__bx.Add(Int16____bw);
            }

            var count__ReadOnlyCountingsButSetable__bD = reader.ReadInt32();
            var ReadOnlyCountingsButSetable__ret__bC = new System.Collections.Generic.List<short>(count__ReadOnlyCountingsButSetable__bD);
            for (int i____bE = 0; i____bE < count__ReadOnlyCountingsButSetable__bD; i____bE++)
            {
                var Int16____bB = reader.ReadInt16();
                ReadOnlyCountingsButSetable__ret__bC.Add(Int16____bB);
            }

            var count__ReadOnlyDicSetable__bI = reader.ReadInt32();
            var ReadOnlyDicSetable__ret__bH = new System.Collections.Generic.Dictionary<short, short>(count__ReadOnlyDicSetable__bI);
            for (int i____bJ = 0; i____bJ < count__ReadOnlyDicSetable__bI; i____bJ++)
            {
                var key__ReadOnlyDicSetable__bF = reader.ReadInt16();
                var value__ReadOnlyDicSetable__bG = reader.ReadInt16();
                ReadOnlyDicSetable__ret__bH.Add(key__ReadOnlyDicSetable__bF, value__ReadOnlyDicSetable__bG);
            }

            var Right__ret__bK = (AccessRight)reader.ReadInt32();
            DEMO.ComplainBase Complain__ret__bS;
            var Complain__Complain__bM = reader.ReadString();
            var count__Complains__bQ = reader.ReadInt32();
            var Complains__Complain__bP = new System.Collections.Generic.List<string>(count__Complains__bQ);
            for (int i____bR = 0; i____bR < count__Complains__bQ; i____bR++)
            {
                var String____bO = reader.ReadString();
                Complains__Complain__bP.Add(String____bO);
            }

            Complain__ret__bS = new DEMO.ComplainBase();
            Complain__ret__bS.Complain = Complain__Complain__bM;
            Complain__ret__bS.Complains = Complains__Complain__bP;
            var AssignedUser__ret__bT = DEMO.SUTMessage.User.DeserializeMe(reader);
            System.Drawing.Point Position__ret__bX;
            var X__Position__bV = reader.ReadInt32();
            var Y__Position__bW = reader.ReadInt32();
            Position__ret__bX = new System.Drawing.Point(X__Position__bV, Y__Position__bW);
            var ContactUser__ret__bY = DEMO.SUTMessage.User.DeserializeMe(reader);
            var AlternativUser__ret__bZ = DEMO.SUTMessage.User.DeserializeMe(reader);
            var X__ret__ca = reader.ReadInt32();
            var countPositions__ret__cb = reader.ReadInt32();
            var randomField__ret__cc = reader.ReadInt32();
            var forCtor__ret__cd = reader.ReadString();
            var ret____ = new DEMO.SUTMessage(forCtor__ret__cd);
            ret____.Positions = Positions__ret__I.ToArray();
            ret____.Type = Type__ret__L;
            ret____.Text = Text__ret__M;
            ret____.UsersList = UsersList__ret__P;
            ret____.ComplainsBases = ComplainsBases__ret__bb;
            ret____.Countings = Countings__ret__bg;
            ret____.CountingDic = CountingDic__ret__bs;
            ret____.ReadOnlyCountingsButSetable = ReadOnlyCountingsButSetable__ret__bC;
            ret____.ReadOnlyDicSetable = ReadOnlyDicSetable__ret__bH;
            ret____.Right = Right__ret__bK;
            ret____.Complain = Complain__ret__bS;
            ret____.AssignedUser = AssignedUser__ret__bT;
            ret____.Position = Position__ret__bX;
            ret____.AlternativUser = AlternativUser__ret__bZ;
            ret____.X = X__ret__ca;
            ret____.countPositions = countPositions__ret__cb;
            ret____.randomField = randomField__ret__cc;
            return ret____;
        }
    }
}