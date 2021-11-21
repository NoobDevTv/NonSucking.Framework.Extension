//namespace DEMO
//{
//    public partial class SUTMessage
//    {
//        public void Serialize(System.IO.BinaryWriter writer)
//        {
//            writer.Write((int)Right);

//            writer.Write(Complain.Complain);

//            writer.Write(Complain.Complains.Count);
//            foreach (var item___7274898e0f6443b385d521f248fc0fb0 in Complain.Complains)
//            {
//                writer.Write(item___7274898e0f6443b385d521f248fc0fb0);
//            }



//            writer.Write(CountingDic.Count);
//            foreach (var item___92d7ce6325d64800bb23b281b0d6c091 in CountingDic)
//            {
//                writer.Write(item___92d7ce6325d64800bb23b281b0d6c091.Key);

//                writer.Write(item___92d7ce6325d64800bb23b281b0d6c091.Value.Complain);

//                writer.Write(item___92d7ce6325d64800bb23b281b0d6c091.Value.Complains.Count);
//                foreach (var item___3e9b0e50f2f64dec907e0aaa15b53443 in item___92d7ce6325d64800bb23b281b0d6c091.Value.Complains)
//                {
//                    writer.Write(item___3e9b0e50f2f64dec907e0aaa15b53443);
//                }


//            }


//            writer.Write(X);
//            writer.Write(countPositions);

//            writer.Write(ReadOnlyCountings.Count);
//            foreach (var item___0ed288f3d1c94903b4566e73a0ccc32c in ReadOnlyCountings)
//            {
//                writer.Write(item___0ed288f3d1c94903b4566e73a0ccc32c);
//            }


//            writer.Write(ContactUser.Name);


//            writer.Write(AlternativUser.Name);


//            writer.Write(ComplainsBases.Count);
//            foreach (var item___6cc00fa48a4c48dfb297707bb06feb49 in ComplainsBases)
//            {

//                writer.Write(item___6cc00fa48a4c48dfb297707bb06feb49.Complain);

//                writer.Write(item___6cc00fa48a4c48dfb297707bb06feb49.Complains.Count);
//                foreach (var item___81ad6b57585747559d7372ea14d81d75 in item___6cc00fa48a4c48dfb297707bb06feb49.Complains)
//                {
//                    writer.Write(item___81ad6b57585747559d7372ea14d81d75);
//                }


//            }


//            writer.Write(Countings.Count);
//            foreach (var item___aeeccf648fc14d2cb9a223c1eee60d9c in Countings)
//            {
//                writer.Write(item___aeeccf648fc14d2cb9a223c1eee60d9c);
//            }


//            writer.Write(Users.Count);
//            foreach (var item___61f8c9c546a04bf2bca024f6599fc694 in Users)
//            {
//                item___61f8c9c546a04bf2bca024f6599fc694.Serialize(writer);
//            }


//            writer.Write(Position.IsEmpty);
//            writer.Write(Position.X);
//            writer.Write(Position.Y);


//            writer.Write(Positions.Length);
//            foreach (var item___04654e7a7ec34cb38e01a3806e021ae3 in Positions)
//            {

//                writer.Write(item___04654e7a7ec34cb38e01a3806e021ae3.IsEmpty);
//                writer.Write(item___04654e7a7ec34cb38e01a3806e021ae3.X);
//                writer.Write(item___04654e7a7ec34cb38e01a3806e021ae3.Y);

//            }


//            writer.Write(Text);
//            AssignedUser.Serialize(writer);
//        }

//        public static SUTMessage Deserialize(System.IO.BinaryReader reader)
//        {
//            var @Right___98f2d0b6fce04605a5c5e0b631c9ba43 = (AccessRight)reader.ReadInt32();

//            DEMO.ComplainBase @Complain___75409a022aa54557b1de7d24dcae9270;
//            {
//                var @Complain___023c304385dd450ebdd7d0b60a7f92e4 = reader.ReadString();

//                var countComplains___b41a48167658494f9e7713b8f3ceed7c = reader.ReadInt32();
//                var @Complains___8bb012d666a342d5916d5d94a5f69829 = new System.Collections.Generic.List<string>(countComplains___b41a48167658494f9e7713b8f3ceed7c);
//                for (int i___f51eddcd4e994355993ba021d3fab42e = 0; i___f51eddcd4e994355993ba021d3fab42e < countComplains___b41a48167658494f9e7713b8f3ceed7c; i___f51eddcd4e994355993ba021d3fab42e++)
//                {
//                    var @String___5f0f12ff98814e64a84ed557bbc0320d = reader.ReadString();
//                    @Complains___8bb012d666a342d5916d5d94a5f69829.Add(@String___5f0f12ff98814e64a84ed557bbc0320d);
//                }


//                @Complain___75409a022aa54557b1de7d24dcae9270 = new DEMO.ComplainBase();

//                @Complain___75409a022aa54557b1de7d24dcae9270.Complain = Complain___023c304385dd450ebdd7d0b60a7f92e4;
//                @Complain___75409a022aa54557b1de7d24dcae9270.Complains = Complains___8bb012d666a342d5916d5d94a5f69829;


//            }


//            var countCountingDic___ea5564bae2cd491d84ab3fc58f5456cf = reader.ReadInt32();
//            var @CountingDic___c167fc98734b4449a25b704a61dd5a97 = new System.Collections.Generic.Dictionary<short, DEMO.ComplainBase>(countCountingDic___ea5564bae2cd491d84ab3fc58f5456cf);
//            for (int i___769fb36041e4491db0649a5289e78679 = 0; i___769fb36041e4491db0649a5289e78679 < countCountingDic___ea5564bae2cd491d84ab3fc58f5456cf; i___769fb36041e4491db0649a5289e78679++)
//            {
//                var @key___295f25eac2484aa9bd0e2d8558dd3668 = reader.ReadInt16();

//                DEMO.ComplainBase @value___c6c4a4b5ae884e7d94ec6e080ffca2f9;
//                {
//                    var @Complain___7c8f84d68046422085570dcd8d8ddcdb = reader.ReadString();

//                    var countComplains___ae24078c5d334665a7cdcac3997f3e63 = reader.ReadInt32();
//                    var @Complains___d8fe8a8831584372aab2e3bcc33ae2c8 = new System.Collections.Generic.List<string>(countComplains___ae24078c5d334665a7cdcac3997f3e63);
//                    for (int i___a138d822e02d4253b5ead3f244401e09 = 0; i___a138d822e02d4253b5ead3f244401e09 < countComplains___ae24078c5d334665a7cdcac3997f3e63; i___a138d822e02d4253b5ead3f244401e09++)
//                    {
//                        var @String___41f3eb9fbc7445e09191023ff9be740a = reader.ReadString();
//                        @Complains___d8fe8a8831584372aab2e3bcc33ae2c8.Add(@String___41f3eb9fbc7445e09191023ff9be740a);
//                    }


//                    @value___c6c4a4b5ae884e7d94ec6e080ffca2f9 = new DEMO.ComplainBase();

//                    @value___c6c4a4b5ae884e7d94ec6e080ffca2f9.Complain = Complain___7c8f84d68046422085570dcd8d8ddcdb;
//                    @value___c6c4a4b5ae884e7d94ec6e080ffca2f9.Complains = Complains___d8fe8a8831584372aab2e3bcc33ae2c8;


//                }


//                @CountingDic___c167fc98734b4449a25b704a61dd5a97.Add(key___295f25eac2484aa9bd0e2d8558dd3668, value___c6c4a4b5ae884e7d94ec6e080ffca2f9);
//            }


//            var @X___70cb5ccbf6ac41b3af09f585f18192bf = reader.ReadInt32();
//            var @countPositions___bb390f8e97ce4b7b942165d2497b4cad = reader.ReadInt32();

//            var countReadOnlyCountings___d588808b64114d55827e84e892f8c67f = reader.ReadInt32();
//            var @ReadOnlyCountings___3a5b1c720b9d4f59a437aaf7991e04ad = new System.Collections.Generic.List<short>(countReadOnlyCountings___d588808b64114d55827e84e892f8c67f);
//            for (int i___4883018e0b194325a622043c6a96366f = 0; i___4883018e0b194325a622043c6a96366f < countReadOnlyCountings___d588808b64114d55827e84e892f8c67f; i___4883018e0b194325a622043c6a96366f++)
//            {
//                var @Int16___4c6bcf557a3d483d92f177d8e9263d66 = reader.ReadInt16();
//                @ReadOnlyCountings___3a5b1c720b9d4f59a437aaf7991e04ad.Add(@Int16___4c6bcf557a3d483d92f177d8e9263d66);
//            }


//            DEMO.IUser @ContactUser___3d61dcdea7d54f158bf3a6c57391994d;
//            {
//                var @Name___727bec83fdcb4d77af232479a77c7d57 = reader.ReadString();
//                @ContactUser___3d61dcdea7d54f158bf3a6c57391994d = default;
//            }


//            DEMO.IUser @AlternativUser___23579bf9d3264b6dabd289dd026cd2a3;
//            {
//                var @Name___144c17a797d6484c8e370704729ab5fd = reader.ReadString();
//                @AlternativUser___23579bf9d3264b6dabd289dd026cd2a3 = default;
//            }


//            var countComplainsBases___cca60962460a456f8a221da167e0ca2c = reader.ReadInt32();
//            var @ComplainsBases___e6e0f62db1864d88bb7d4463e8d8a90f = new System.Collections.Generic.List<DEMO.ComplainBase>(countComplainsBases___cca60962460a456f8a221da167e0ca2c);
//            for (int i___c8368f2cd4da4f7bbc53e294b468f615 = 0; i___c8368f2cd4da4f7bbc53e294b468f615 < countComplainsBases___cca60962460a456f8a221da167e0ca2c; i___c8368f2cd4da4f7bbc53e294b468f615++)
//            {

//                DEMO.ComplainBase @ComplainBase______5af50d3c950843c6830c777f20d57a72;
//                {
//                    var @Complain___dcb2d81bf2374a009e667d954a61b65d = reader.ReadString();

//                    var countComplains___9b7316dff30c467c86e44694c7fdcaee = reader.ReadInt32();
//                    var @Complains___8404898220c1444b8da3cc7f4dc11b0c = new System.Collections.Generic.List<string>(countComplains___9b7316dff30c467c86e44694c7fdcaee);
//                    for (int i___35ee84282d47495d91efea785c3995e0 = 0; i___35ee84282d47495d91efea785c3995e0 < countComplains___9b7316dff30c467c86e44694c7fdcaee; i___35ee84282d47495d91efea785c3995e0++)
//                    {
//                        var @String___0206afcc185a41ed98131780221757fc = reader.ReadString();
//                        @Complains___8404898220c1444b8da3cc7f4dc11b0c.Add(@String___0206afcc185a41ed98131780221757fc);
//                    }


//                    @ComplainBase______5af50d3c950843c6830c777f20d57a72 = new DEMO.ComplainBase();

//                    @ComplainBase______5af50d3c950843c6830c777f20d57a72.Complain = Complain___dcb2d81bf2374a009e667d954a61b65d;
//                    @ComplainBase______5af50d3c950843c6830c777f20d57a72.Complains = Complains___8404898220c1444b8da3cc7f4dc11b0c;


//                }


//                @ComplainsBases___e6e0f62db1864d88bb7d4463e8d8a90f.Add(ComplainBase______5af50d3c950843c6830c777f20d57a72);
//            }


//            var countCountings___ef88aadfff5b4bb9a87690b3a5335914 = reader.ReadInt32();
//            var @Countings___fbb28628492f479d83190a82bd1ab480 = new System.Collections.Generic.List<short>(countCountings___ef88aadfff5b4bb9a87690b3a5335914);
//            for (int i___a5e85632c8bc49afaa989a08941ea338 = 0; i___a5e85632c8bc49afaa989a08941ea338 < countCountings___ef88aadfff5b4bb9a87690b3a5335914; i___a5e85632c8bc49afaa989a08941ea338++)
//            {
//                var @Int16___c2f7bec88b2143e3aae987f6b1f416e0 = reader.ReadInt16();
//                @Countings___fbb28628492f479d83190a82bd1ab480.Add(@Int16___c2f7bec88b2143e3aae987f6b1f416e0);
//            }


//            var countUsers___93d86ac64484484d9ff6c2b9c37cdb68 = reader.ReadInt32();
//            var @Users___831e7dcc6f9e473cbbfe9e23187ec16f = new System.Collections.Generic.List<DEMO.SUTMessage.User>(countUsers___93d86ac64484484d9ff6c2b9c37cdb68);
//            for (int i___e7b913e4a98342749dacb9d67fe5c5ab = 0; i___e7b913e4a98342749dacb9d67fe5c5ab < countUsers___93d86ac64484484d9ff6c2b9c37cdb68; i___e7b913e4a98342749dacb9d67fe5c5ab++)
//            {
//                var @User______d997514b19404565a132d08c47f547c9 = DEMO.SUTMessage.User.Deserialize(reader);
//                @Users___831e7dcc6f9e473cbbfe9e23187ec16f.Add(User______d997514b19404565a132d08c47f547c9);
//            }


//            System.Drawing.Point @Position___6962b878c25046beb11319bbbc904cd5;
//            {
//                var @IsEmpty___b5600387ad5b491db9ebb27d23feb40f = reader.ReadBoolean();
//                var @X___f5a6fe5957a345adbd46e7503f849a3b = reader.ReadInt32();
//                var @Y___ce469b35716842b8b2b044cd09045519 = reader.ReadInt32();

//                @Position___6962b878c25046beb11319bbbc904cd5 = new System.Drawing.Point(X___f5a6fe5957a345adbd46e7503f849a3b, Y___ce469b35716842b8b2b044cd09045519);



//            }


//            var countPositions___f4059429467f45beb7197765f5acc486 = reader.ReadInt32();
//            var @Positions___0a286af39b204f11ba60e8e1845f03fd = new System.Collections.Generic.List<System.Drawing.Point>(countPositions___f4059429467f45beb7197765f5acc486);
//            for (int i___17e17fc28ee54aaeb4b5cece4cff8cac = 0; i___17e17fc28ee54aaeb4b5cece4cff8cac < countPositions___f4059429467f45beb7197765f5acc486; i___17e17fc28ee54aaeb4b5cece4cff8cac++)
//            {

//                System.Drawing.Point @Point______7bbc57f122804d3e9c82f91bb3356f7d;
//                {
//                    var @IsEmpty___3f4e38b0f0634bce98791b532afd913c = reader.ReadBoolean();
//                    var @X___a379ce8a1ede4db6a3c3277e990a5c5f = reader.ReadInt32();
//                    var @Y___46c619bc42a24cd095a1d31ba102734e = reader.ReadInt32();

//                    @Point______7bbc57f122804d3e9c82f91bb3356f7d = new System.Drawing.Point(X___a379ce8a1ede4db6a3c3277e990a5c5f, Y___46c619bc42a24cd095a1d31ba102734e);



//                }


//                @Positions___0a286af39b204f11ba60e8e1845f03fd.Add(Point______7bbc57f122804d3e9c82f91bb3356f7d);
//            }


//            var @Text___a0b43dfa5361489ea14b57574e463f46 = reader.ReadString();
//            var @AssignedUser___42228d65a2a44821b7ae12792a2bff90 = DEMO.SUTMessage.User.Deserialize(reader);

//            var returnValue___292a1b5049bb442db2b294a899b91ce1 = new DEMO.SUTMessage();

//            returnValue___292a1b5049bb442db2b294a899b91ce1.Positions = Positions___0a286af39b204f11ba60e8e1845f03fd.ToArray();
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Text = Text___a0b43dfa5361489ea14b57574e463f46;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Users = Users___831e7dcc6f9e473cbbfe9e23187ec16f;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.ComplainsBases = ComplainsBases___e6e0f62db1864d88bb7d4463e8d8a90f;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Countings = Countings___fbb28628492f479d83190a82bd1ab480;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.CountingDic = CountingDic___c167fc98734b4449a25b704a61dd5a97;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Right = Right___98f2d0b6fce04605a5c5e0b631c9ba43;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Complain = Complain___75409a022aa54557b1de7d24dcae9270;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.AssignedUser = AssignedUser___42228d65a2a44821b7ae12792a2bff90;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.Position = Position___6962b878c25046beb11319bbbc904cd5;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.AlternativUser = AlternativUser___23579bf9d3264b6dabd289dd026cd2a3;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.X = X___70cb5ccbf6ac41b3af09f585f18192bf;
//            returnValue___292a1b5049bb442db2b294a899b91ce1.countPositions = countPositions___bb390f8e97ce4b7b942165d2497b4cad;


//            return returnValue___292a1b5049bb442db2b294a899b91ce1;
//        }
//    }
//}