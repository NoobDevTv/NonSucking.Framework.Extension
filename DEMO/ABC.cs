//namespace DEMO
//{
//    public partial class SUTMessage
//    {
//        public void Serialize(System.IO.BinaryWriter writer)
//        {

//            writer.Write(Positions.Length);
//            foreach (var item___2c9c3b47b6a64f949f87314c4b4811a8 in Positions)
//            {

//                writer.Write(item___2c9c3b47b6a64f949f87314c4b4811a8.IsEmpty);
//                writer.Write(item___2c9c3b47b6a64f949f87314c4b4811a8.X);
//                writer.Write(item___2c9c3b47b6a64f949f87314c4b4811a8.Y);

//            }


//            writer.Write(Text);

//            writer.Write(UsersList.Count);
//            foreach (var item___d0736c6a3ae04076ad4f71d75e5240fd in UsersList)
//            {
//                item___d0736c6a3ae04076ad4f71d75e5240fd.SerializeMe(writer);
//            }


//            writer.Write(ComplainsBases.Count);
//            foreach (var item___eea9d5e15a60455186a827c78692df42 in ComplainsBases)
//            {

//                writer.Write(item___eea9d5e15a60455186a827c78692df42.Complain);

//                writer.Write(item___eea9d5e15a60455186a827c78692df42.Complains.Count);
//                foreach (var item___edf7427aa463498481fa034e2f771015 in item___eea9d5e15a60455186a827c78692df42.Complains)
//                {
//                    writer.Write(item___edf7427aa463498481fa034e2f771015);
//                }


//            }


//            writer.Write(Countings.Count);
//            foreach (var item___77b67d99f38340f7a1182155b901d1d5 in Countings)
//            {
//                writer.Write(item___77b67d99f38340f7a1182155b901d1d5);
//            }


//            writer.Write(CountingDic.Count);
//            foreach (var item___9803905a0a2e48b08201e0ec339c87c0 in CountingDic)
//            {
//                writer.Write(item___9803905a0a2e48b08201e0ec339c87c0.Key);

//                writer.Write(item___9803905a0a2e48b08201e0ec339c87c0.Value.Complain);

//                writer.Write(item___9803905a0a2e48b08201e0ec339c87c0.Value.Complains.Count);
//                foreach (var item___a85791352498490f8b8eb4241e2d342f in item___9803905a0a2e48b08201e0ec339c87c0.Value.Complains)
//                {
//                    writer.Write(item___a85791352498490f8b8eb4241e2d342f);
//                }


//            }


//            writer.Write(ReadOnlyCountings.Count);
//            foreach (var item___1f2fb634576f4a8caadf4f2b6a57be85 in ReadOnlyCountings)
//            {
//                writer.Write(item___1f2fb634576f4a8caadf4f2b6a57be85);
//            }


//            writer.Write((int)Right);

//            writer.Write(Complain.Complain);

//            writer.Write(Complain.Complains.Count);
//            foreach (var item___8bce757b754a4d5da0c4ad674f54b697 in Complain.Complains)
//            {
//                writer.Write(item___8bce757b754a4d5da0c4ad674f54b697);
//            }



//            AssignedUser.SerializeMe(writer);

//            writer.Write(Position.IsEmpty);
//            writer.Write(Position.X);
//            writer.Write(Position.Y);


//            writer.Write(ContactUser.Name);


//            writer.Write(AlternativUser.Name);


//            writer.Write(X);
//            writer.Write(countPositions);
//        }

//        public static SUTMessage Deserialize(System.IO.BinaryReader reader)
//        {

//            var countPositions___b3fe4c6db0ac4d28b1ad7c4dbe4ae0c9 = reader.ReadInt32();
//            var @Positions___42440040ebe74f148d8ecfad5479167a = new System.Collections.Generic.List<System.Drawing.Point>(countPositions___b3fe4c6db0ac4d28b1ad7c4dbe4ae0c9);
//            for (int i___8792594a83694733aa0eee122170101e = 0; i___8792594a83694733aa0eee122170101e < countPositions___b3fe4c6db0ac4d28b1ad7c4dbe4ae0c9; i___8792594a83694733aa0eee122170101e++)
//            {

//                System.Drawing.Point @Point___122cb66a470841a38c62fc84e9458368;
//                {
//                    var @IsEmpty___0c9decf8ed684e36a9dfe35ed3f555dd = reader.ReadBoolean();
//                    var @X___c60a4afdc63046f3acac9a9686d46299 = reader.ReadInt32();
//                    var @Y___bf153f06f105458692f173c473483c64 = reader.ReadInt32();

//                    @Point___122cb66a470841a38c62fc84e9458368 = new System.Drawing.Point(X___c60a4afdc63046f3acac9a9686d46299, Y___bf153f06f105458692f173c473483c64);



//                }


//                @Positions___42440040ebe74f148d8ecfad5479167a.Add(@Point___122cb66a470841a38c62fc84e9458368);
//            }


//            var @Text___f53e0845bc35444fb1a28eb50dbf7585 = reader.ReadString();

//            var countUsersList___6188c1ca65c146f78b26b88ddab77558 = reader.ReadInt32();
//            var @UsersList___0d4cf4a53442436285bb51690f03eae9 = new System.Collections.Generic.List<DEMO.SUTMessage.User>(countUsersList___6188c1ca65c146f78b26b88ddab77558);
//            for (int i___f408c9828c264bc6b38096f59c1bc8a3 = 0; i___f408c9828c264bc6b38096f59c1bc8a3 < countUsersList___6188c1ca65c146f78b26b88ddab77558; i___f408c9828c264bc6b38096f59c1bc8a3++)
//            {
//                var @User___4e2f38bcab134f93828ead6417dd85af = DEMO.SUTMessage.User.DeserializeMe(reader);
//                @UsersList___0d4cf4a53442436285bb51690f03eae9.Add(@User___4e2f38bcab134f93828ead6417dd85af);
//            }


//            var countComplainsBases___da2359968d4d4db7abb4cbdbcd4d82b9 = reader.ReadInt32();
//            var @ComplainsBases___9f6fbd6ba1334b7aa35304b78bc4244a = new System.Collections.Generic.List<DEMO.ComplainBase>(countComplainsBases___da2359968d4d4db7abb4cbdbcd4d82b9);
//            for (int i___5f1e9ca496a548b4a9c6f325d6ef0df7 = 0; i___5f1e9ca496a548b4a9c6f325d6ef0df7 < countComplainsBases___da2359968d4d4db7abb4cbdbcd4d82b9; i___5f1e9ca496a548b4a9c6f325d6ef0df7++)
//            {

//                DEMO.ComplainBase @ComplainBase___64916bf694ed4111bd8106bacbbfa566;
//                {
//                    var @Complain___aeffd2623f5a4417be401ba7b5e6bcf0 = reader.ReadString();

//                    var countComplains___c86f8a4f0bb8401abda8db559fff9405 = reader.ReadInt32();
//                    var @Complains___7ba9f3fc3c6648478ab29c3dc24783cf = new System.Collections.Generic.List<string>(countComplains___c86f8a4f0bb8401abda8db559fff9405);
//                    for (int i___b2d4fbc9b1af42c9acf67762017920e8 = 0; i___b2d4fbc9b1af42c9acf67762017920e8 < countComplains___c86f8a4f0bb8401abda8db559fff9405; i___b2d4fbc9b1af42c9acf67762017920e8++)
//                    {
//                        var @String___c8bc4fa0b8eb45cb96b0f9d5be54538b = reader.ReadString();
//                        @Complains___7ba9f3fc3c6648478ab29c3dc24783cf.Add(@String___c8bc4fa0b8eb45cb96b0f9d5be54538b);
//                    }


//                    @ComplainBase___64916bf694ed4111bd8106bacbbfa566 = new DEMO.ComplainBase();

//                    @ComplainBase___64916bf694ed4111bd8106bacbbfa566.Complain = Complain___aeffd2623f5a4417be401ba7b5e6bcf0;
//                    @ComplainBase___64916bf694ed4111bd8106bacbbfa566.Complains = Complains___7ba9f3fc3c6648478ab29c3dc24783cf;


//                }


//                @ComplainsBases___9f6fbd6ba1334b7aa35304b78bc4244a.Add(@ComplainBase___64916bf694ed4111bd8106bacbbfa566);
//            }


//            var countCountings___9a8461ede73742c787f042ed6e3c07a5 = reader.ReadInt32();
//            var @Countings___ec1b5d1c70fc4883a4405ec4baeabe98 = new System.Collections.Generic.List<short>(countCountings___9a8461ede73742c787f042ed6e3c07a5);
//            for (int i___618c559144a547d2ad53d0b49b087d8f = 0; i___618c559144a547d2ad53d0b49b087d8f < countCountings___9a8461ede73742c787f042ed6e3c07a5; i___618c559144a547d2ad53d0b49b087d8f++)
//            {
//                var @Int16___99ca8f7fe59d4ef490b3432b5288c2c7 = reader.ReadInt16();
//                @Countings___ec1b5d1c70fc4883a4405ec4baeabe98.Add(@Int16___99ca8f7fe59d4ef490b3432b5288c2c7);
//            }


//            var countCountingDic___224ea14074af4297b5a06e9008ee2340 = reader.ReadInt32();
//            var @CountingDic___a4b3e85e525345ecb5b30c97c59170ef = new System.Collections.Generic.Dictionary<short, DEMO.ComplainBase>(countCountingDic___224ea14074af4297b5a06e9008ee2340);
//            for (int i___2fa00c43fe4d423d813cc50545c26007 = 0; i___2fa00c43fe4d423d813cc50545c26007 < countCountingDic___224ea14074af4297b5a06e9008ee2340; i___2fa00c43fe4d423d813cc50545c26007++)
//            {
//                var @key___eb59a5dcc869403ba43d2d4f0a583c62 = reader.ReadInt16();

//                DEMO.ComplainBase @value___b093c4b7897d4211b602e0a3590584b2;
//                {
//                    var @Complain___100bee7bca8c4e628e4839ff23ce5e0d = reader.ReadString();

//                    var countComplains___97c13418057940d09548ad6fec4b91dd = reader.ReadInt32();
//                    var @Complains___799743c0efd54baf9b53873318fc4b18 = new System.Collections.Generic.List<string>(countComplains___97c13418057940d09548ad6fec4b91dd);
//                    for (int i___56cab966f59a42e2afb7a647ae4af38c = 0; i___56cab966f59a42e2afb7a647ae4af38c < countComplains___97c13418057940d09548ad6fec4b91dd; i___56cab966f59a42e2afb7a647ae4af38c++)
//                    {
//                        var @String___6aacf146c2614f31b8a5053e14594e67 = reader.ReadString();
//                        @Complains___799743c0efd54baf9b53873318fc4b18.Add(@String___6aacf146c2614f31b8a5053e14594e67);
//                    }


//                    @value___b093c4b7897d4211b602e0a3590584b2 = new DEMO.ComplainBase();

//                    @value___b093c4b7897d4211b602e0a3590584b2.Complain = Complain___100bee7bca8c4e628e4839ff23ce5e0d;
//                    @value___b093c4b7897d4211b602e0a3590584b2.Complains = Complains___799743c0efd54baf9b53873318fc4b18;


//                }


//                @CountingDic___a4b3e85e525345ecb5b30c97c59170ef.Add(key___eb59a5dcc869403ba43d2d4f0a583c62, value___b093c4b7897d4211b602e0a3590584b2);
//            }


//            var countReadOnlyCountings___b8cdfee9f3ef4416bb337639f1d88ea4 = reader.ReadInt32();
//            var @ReadOnlyCountings___40157a6464214caf8675be97cf80c893 = new System.Collections.Generic.List<short>(countReadOnlyCountings___b8cdfee9f3ef4416bb337639f1d88ea4);
//            for (int i___6f041c0ff2ac411c8c2b868f17d272af = 0; i___6f041c0ff2ac411c8c2b868f17d272af < countReadOnlyCountings___b8cdfee9f3ef4416bb337639f1d88ea4; i___6f041c0ff2ac411c8c2b868f17d272af++)
//            {
//                var @Int16___810fe12da7b643aa80a01facb821b3a0 = reader.ReadInt16();
//                @ReadOnlyCountings___40157a6464214caf8675be97cf80c893.Add(@Int16___810fe12da7b643aa80a01facb821b3a0);
//            }


//            var @Right___de7fb3c3a3484a79bbcfee937a2a3c9d = (AccessRight)reader.ReadInt32();

//            DEMO.ComplainBase @Complain___b1d0b6868aa44c76acae71fa08ec1559;
//            {
//                var @Complain___752ce844f00341bebb9a333b6fbc432e = reader.ReadString();

//                var countComplains___b6235a3afd65408aaa7e1a5be9efe118 = reader.ReadInt32();
//                var @Complains___a879a3340a3948d58796237f43d88ca2 = new System.Collections.Generic.List<string>(countComplains___b6235a3afd65408aaa7e1a5be9efe118);
//                for (int i___3a92251f3f7b4a11ad5ac55d0c4c3507 = 0; i___3a92251f3f7b4a11ad5ac55d0c4c3507 < countComplains___b6235a3afd65408aaa7e1a5be9efe118; i___3a92251f3f7b4a11ad5ac55d0c4c3507++)
//                {
//                    var @String___de69610e750442eea73b24b3f8d6f638 = reader.ReadString();
//                    @Complains___a879a3340a3948d58796237f43d88ca2.Add(@String___de69610e750442eea73b24b3f8d6f638);
//                }


//                @Complain___b1d0b6868aa44c76acae71fa08ec1559 = new DEMO.ComplainBase();

//                @Complain___b1d0b6868aa44c76acae71fa08ec1559.Complain = Complain___752ce844f00341bebb9a333b6fbc432e;
//                @Complain___b1d0b6868aa44c76acae71fa08ec1559.Complains = Complains___a879a3340a3948d58796237f43d88ca2;


//            }


//            var @AssignedUser___418d118f497a4b0894fd3aeac07fa7b4 = DEMO.SUTMessage.User.DeserializeMe(reader);

//            System.Drawing.Point @Position___3be4d91e4844493f92f29ef2801c39b1;
//            {
//                var @IsEmpty___2cb15ba260fa46f98d8abe74a374bede = reader.ReadBoolean();
//                var @X___119f8a3248884c30ac0003ba5995eb54 = reader.ReadInt32();
//                var @Y___b6148dd5081f4565af2ad9dfa2dcd075 = reader.ReadInt32();

//                @Position___3be4d91e4844493f92f29ef2801c39b1 = new System.Drawing.Point(X___119f8a3248884c30ac0003ba5995eb54, Y___b6148dd5081f4565af2ad9dfa2dcd075);



//            }


//            DEMO.IUser @ContactUser___560f8daa66b44b398ce9af0ae3a7a652;
//            {
//                var @Name___84708c0b836c4ba19465091def3800bc = reader.ReadString();
//                @ContactUser___560f8daa66b44b398ce9af0ae3a7a652 = default;
//            }


//            DEMO.IUser @AlternativUser___61a963f43a204a448b82872d4890edb7;
//            {
//                var @Name___d077229cc7374067b670dbad2c0d7dd9 = reader.ReadString();
//                @AlternativUser___61a963f43a204a448b82872d4890edb7 = default;
//            }


//            var @X___c3ac69e7798f41e3987a41acc585857b = reader.ReadInt32();
//            var @countPositions___b140236c4fe6477a9b0b3c130b715338 = reader.ReadInt32();

//            var returnValue___4282e785524240f0a10aba1aaa8df738 = new DEMO.SUTMessage();

//            returnValue___4282e785524240f0a10aba1aaa8df738.Positions = Positions___42440040ebe74f148d8ecfad5479167a.ToArray();
//            returnValue___4282e785524240f0a10aba1aaa8df738.Text = Text___f53e0845bc35444fb1a28eb50dbf7585;
//            returnValue___4282e785524240f0a10aba1aaa8df738.UsersList = UsersList___0d4cf4a53442436285bb51690f03eae9;
//            returnValue___4282e785524240f0a10aba1aaa8df738.ComplainsBases = ComplainsBases___9f6fbd6ba1334b7aa35304b78bc4244a;
//            returnValue___4282e785524240f0a10aba1aaa8df738.Countings = Countings___ec1b5d1c70fc4883a4405ec4baeabe98;
//            returnValue___4282e785524240f0a10aba1aaa8df738.CountingDic = CountingDic___a4b3e85e525345ecb5b30c97c59170ef;
//            returnValue___4282e785524240f0a10aba1aaa8df738.Right = Right___de7fb3c3a3484a79bbcfee937a2a3c9d;
//            returnValue___4282e785524240f0a10aba1aaa8df738.Complain = Complain___b1d0b6868aa44c76acae71fa08ec1559;
//            returnValue___4282e785524240f0a10aba1aaa8df738.AssignedUser = AssignedUser___418d118f497a4b0894fd3aeac07fa7b4;
//            returnValue___4282e785524240f0a10aba1aaa8df738.Position = Position___3be4d91e4844493f92f29ef2801c39b1;
//            returnValue___4282e785524240f0a10aba1aaa8df738.AlternativUser = AlternativUser___61a963f43a204a448b82872d4890edb7;
//            returnValue___4282e785524240f0a10aba1aaa8df738.X = X___c3ac69e7798f41e3987a41acc585857b;
//            returnValue___4282e785524240f0a10aba1aaa8df738.countPositions = countPositions___b140236c4fe6477a9b0b3c130b715338;


//            return returnValue___4282e785524240f0a10aba1aaa8df738;
//        }
//    }
//}