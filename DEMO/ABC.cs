//namespace DEMO
//{
//    public partial class SUTMessage
//    {
//        public void Serialize(System.IO.BinaryWriter writer)
//        {

//            writer.Write(Positions.Length);
//            foreach (var item___00465bb861654173bd2dcc3d17ecf599 in Positions)
//            {

//                writer.Write(item___00465bb861654173bd2dcc3d17ecf599);
//                writer.Write(item___00465bb861654173bd2dcc3d17ecf599);
//                writer.Write(item___00465bb861654173bd2dcc3d17ecf599);

//            }


//            writer.Write(Text);

//            writer.Write(UsersList.Count);
//            foreach (var item___ca577bad3eb2438e84d7ec06a3a09ffb in UsersList)
//            {
//                item___ca577bad3eb2438e84d7ec06a3a09ffb.SerializeMe(writer);
//            }


//            writer.Write(ComplainsBases.Count);
//            foreach (var item___137ba30dbecd4501b9df0531fe283915 in ComplainsBases)
//            {

//                writer.Write(item___137ba30dbecd4501b9df0531fe283915);

//                writer.Write(item___137ba30dbecd4501b9df0531fe283915.Count);
//                foreach (var item___e8a45ae2d3c945c597d3d8e09b3de1a4 in item___137ba30dbecd4501b9df0531fe283915)
//                {
//                    writer.Write(item___e8a45ae2d3c945c597d3d8e09b3de1a4);
//                }


//            }


//            writer.Write(Countings.Count);
//            foreach (var item___d123dcb8794340038abf3c7f34e313b2 in Countings)
//            {
//                writer.Write(item___d123dcb8794340038abf3c7f34e313b2);
//            }


//            writer.Write(CountingDic.Count);
//            foreach (var item___737c25c8b0bb4ebda090486bb0552c90 in CountingDic)
//            {
//                writer.Write(item___737c25c8b0bb4ebda090486bb0552c90.Key);

//                writer.Write(item___737c25c8b0bb4ebda090486bb0552c90.Value);

//                writer.Write(item___737c25c8b0bb4ebda090486bb0552c90.Value.Count);
//                foreach (var item___3f3c85e009714431929219429e8e9c37 in item___737c25c8b0bb4ebda090486bb0552c90.Value)
//                {
//                    writer.Write(item___3f3c85e009714431929219429e8e9c37);
//                }


//            }


//            writer.Write(ReadOnlyCountings.Count);
//            foreach (var item___d8a8e9669fd940afa681e329fbc61224 in ReadOnlyCountings)
//            {
//                writer.Write(item___d8a8e9669fd940afa681e329fbc61224);
//            }


//            writer.Write(ReadOnlyCountingsButSetable.Count);
//            foreach (var item___3e691c591a5f4dd7ae5786850737df5f in ReadOnlyCountingsButSetable)
//            {
//                writer.Write(item___3e691c591a5f4dd7ae5786850737df5f);
//            }


//            writer.Write((int)Right);

//            writer.Write(Complain);

//            writer.Write(Complain.Count);
//            foreach (var item___133bf16ee6c54a799e070a47b2b433af in Complain)
//            {
//                writer.Write(item___133bf16ee6c54a799e070a47b2b433af);
//            }



//            AssignedUser.SerializeMe(writer);

//            writer.Write(Position);
//            writer.Write(Position);
//            writer.Write(Position);


//            DEMO.SUTMessage.User.SerializeMe(writer, ContactUser);
//            DEMO.SUTMessage.User.SerializeMe(writer, AlternativUser);
//            writer.Write(X);
//            writer.Write(countPositions);
//        }

//        public static SUTMessage Deserialize(System.IO.BinaryReader reader)
//        {

//            var countPositions___08e553870d464679ad7cd408e046c768 = reader.ReadInt32();
//            var @Positions___af785b88f201418fbb64e6bacb60bb71 = new System.Collections.Generic.List<System.Drawing.Point>(countPositions___08e553870d464679ad7cd408e046c768);
//            for (int i___bff459de96f84e0a8b55070d7619d3d8 = 0; i___bff459de96f84e0a8b55070d7619d3d8 < countPositions___08e553870d464679ad7cd408e046c768; i___bff459de96f84e0a8b55070d7619d3d8++)
//            {

//                System.Drawing.Point @Point___b7b996746025442686855337a4731681;
//                {
//                    var @IsEmpty___3eea643f9eb64353a99e07480ae3e8b5 = reader.ReadBoolean();
//                    var @X___479243cddc8146ecbb52553008056d2b = reader.ReadInt32();
//                    var @Y___74fe9a617cc34f2da39d244266263c2d = reader.ReadInt32();

//                    @Point___b7b996746025442686855337a4731681 = new System.Drawing.Point(X___479243cddc8146ecbb52553008056d2b, Y___74fe9a617cc34f2da39d244266263c2d);



//                }


//                @Positions___af785b88f201418fbb64e6bacb60bb71.Add(@Point___b7b996746025442686855337a4731681);
//            }


//            var @Text___567eb6da2a4a40b8839f32682323aef9 = reader.ReadString();

//            var countUsersList___1661e6ceb3ce4d26bf9a229720b4a7d5 = reader.ReadInt32();
//            var @UsersList___dcee38cccd5640cba39ed970cac813a3 = new System.Collections.Generic.List<DEMO.SUTMessage.User>(countUsersList___1661e6ceb3ce4d26bf9a229720b4a7d5);
//            for (int i___e09b5774d550469bb0a5b8b5b5ad54c0 = 0; i___e09b5774d550469bb0a5b8b5b5ad54c0 < countUsersList___1661e6ceb3ce4d26bf9a229720b4a7d5; i___e09b5774d550469bb0a5b8b5b5ad54c0++)
//            {
//                var @User___ba858ff4eec940eabea8949e11775145 = DEMO.SUTMessage.User.DeserializeMe(reader);
//                @UsersList___dcee38cccd5640cba39ed970cac813a3.Add(@User___ba858ff4eec940eabea8949e11775145);
//            }


//            var countComplainsBases___c0fd4e152b584e119b48fe228e6ffbd0 = reader.ReadInt32();
//            var @ComplainsBases___a9ab8aaeaafa43c79abca40cc4d9a8d0 = new System.Collections.Generic.List<DEMO.ComplainBase>(countComplainsBases___c0fd4e152b584e119b48fe228e6ffbd0);
//            for (int i___c13cddcd5d4d4f4e8427de32101bfb6f = 0; i___c13cddcd5d4d4f4e8427de32101bfb6f < countComplainsBases___c0fd4e152b584e119b48fe228e6ffbd0; i___c13cddcd5d4d4f4e8427de32101bfb6f++)
//            {

//                DEMO.ComplainBase @ComplainBase___9620b125ba7b46ebb88a00e1e8330ed2;
//                {
//                    var @Complain___7cbed4c995ac4bf98fe5dd485fd239bd = reader.ReadString();

//                    var countComplains___e6ab9d932e874849bfdd17c4d3a7e0ea = reader.ReadInt32();
//                    var @Complains___4de374f34022427d84d7d1b5f50a3cf7 = new System.Collections.Generic.List<string>(countComplains___e6ab9d932e874849bfdd17c4d3a7e0ea);
//                    for (int i___b39b692c6bd34c6fa6eec223b8f364b6 = 0; i___b39b692c6bd34c6fa6eec223b8f364b6 < countComplains___e6ab9d932e874849bfdd17c4d3a7e0ea; i___b39b692c6bd34c6fa6eec223b8f364b6++)
//                    {
//                        var @String___c701dd6204b940489e28f0b1ef8bd199 = reader.ReadString();
//                        @Complains___4de374f34022427d84d7d1b5f50a3cf7.Add(@String___c701dd6204b940489e28f0b1ef8bd199);
//                    }


//                    @ComplainBase___9620b125ba7b46ebb88a00e1e8330ed2 = new DEMO.ComplainBase();

//                    @ComplainBase___9620b125ba7b46ebb88a00e1e8330ed2.Complain = Complain___7cbed4c995ac4bf98fe5dd485fd239bd;
//                    @ComplainBase___9620b125ba7b46ebb88a00e1e8330ed2.Complains = Complains___4de374f34022427d84d7d1b5f50a3cf7;


//                }


//                @ComplainsBases___a9ab8aaeaafa43c79abca40cc4d9a8d0.Add(@ComplainBase___9620b125ba7b46ebb88a00e1e8330ed2);
//            }


//            var countCountings___e7ddac5d58b44c24baf02cd2424d858c = reader.ReadInt32();
//            var @Countings___9899105a9d774d349cb8a0c05279b1ac = new System.Collections.Generic.List<short>(countCountings___e7ddac5d58b44c24baf02cd2424d858c);
//            for (int i___8b5c2f21f5eb42648e8adc8c1cae6138 = 0; i___8b5c2f21f5eb42648e8adc8c1cae6138 < countCountings___e7ddac5d58b44c24baf02cd2424d858c; i___8b5c2f21f5eb42648e8adc8c1cae6138++)
//            {
//                var @Int16___584437aae39e47c780794af9c38682b3 = reader.ReadInt16();
//                @Countings___9899105a9d774d349cb8a0c05279b1ac.Add(@Int16___584437aae39e47c780794af9c38682b3);
//            }


//            var countCountingDic___6836c2314ef544cda7b5954bb2615b90 = reader.ReadInt32();
//            var @CountingDic___d9b8473c626f4e239c72aed2ff285df1 = new System.Collections.Generic.Dictionary<short, DEMO.ComplainBase>(countCountingDic___6836c2314ef544cda7b5954bb2615b90);
//            for (int i___dee8604e900647098f5ebb97aa49fb9a = 0; i___dee8604e900647098f5ebb97aa49fb9a < countCountingDic___6836c2314ef544cda7b5954bb2615b90; i___dee8604e900647098f5ebb97aa49fb9a++)
//            {
//                var @key___61ba53b0de914e54821c87d4cd2b46db = reader.ReadInt16();

//                DEMO.ComplainBase @value___ab6266a539af4c5a92fdc5fa0226ce63;
//                {
//                    var @Complain___a061480c5952438e935df79512fb2946 = reader.ReadString();

//                    var countComplains___a744efd6a7864a0086945b2b71a156f2 = reader.ReadInt32();
//                    var @Complains___f5ccfddcb8c94a7097e34acfabed15a8 = new System.Collections.Generic.List<string>(countComplains___a744efd6a7864a0086945b2b71a156f2);
//                    for (int i___f616da761ef04116a23b2f1e136076f1 = 0; i___f616da761ef04116a23b2f1e136076f1 < countComplains___a744efd6a7864a0086945b2b71a156f2; i___f616da761ef04116a23b2f1e136076f1++)
//                    {
//                        var @String___30b8279b3bd24d76924e4739c0ba9980 = reader.ReadString();
//                        @Complains___f5ccfddcb8c94a7097e34acfabed15a8.Add(@String___30b8279b3bd24d76924e4739c0ba9980);
//                    }


//                    @value___ab6266a539af4c5a92fdc5fa0226ce63 = new DEMO.ComplainBase();

//                    @value___ab6266a539af4c5a92fdc5fa0226ce63.Complain = Complain___a061480c5952438e935df79512fb2946;
//                    @value___ab6266a539af4c5a92fdc5fa0226ce63.Complains = Complains___f5ccfddcb8c94a7097e34acfabed15a8;


//                }


//                @CountingDic___d9b8473c626f4e239c72aed2ff285df1.Add(key___61ba53b0de914e54821c87d4cd2b46db, value___ab6266a539af4c5a92fdc5fa0226ce63);
//            }


//            var countReadOnlyCountings___e62ed938034a445fabdc3af2f1a772af = reader.ReadInt32();
//            var @ReadOnlyCountings___a553a83447024dd5af633f4e8d93c2c0 = new System.Collections.Generic.List<short>(countReadOnlyCountings___e62ed938034a445fabdc3af2f1a772af);
//            for (int i___94a08fdf49274c0e848d6ea58f2ef6c2 = 0; i___94a08fdf49274c0e848d6ea58f2ef6c2 < countReadOnlyCountings___e62ed938034a445fabdc3af2f1a772af; i___94a08fdf49274c0e848d6ea58f2ef6c2++)
//            {
//                var @Int16___8c3bb266dc5946eca44c94a06ab7e99b = reader.ReadInt16();
//                @ReadOnlyCountings___a553a83447024dd5af633f4e8d93c2c0.Add(@Int16___8c3bb266dc5946eca44c94a06ab7e99b);
//            }


//            var countReadOnlyCountingsButSetable___e8cbcb25bc924f5e91474f39d1dddf9c = reader.ReadInt32();
//            var @ReadOnlyCountingsButSetable___b2f2e14067b24f648fcdd8a503f56d1d = new System.Collections.Generic.List<short>(countReadOnlyCountingsButSetable___e8cbcb25bc924f5e91474f39d1dddf9c);
//            for (int i___0bf4b71909d64427bc1d06d4d4f74d8b = 0; i___0bf4b71909d64427bc1d06d4d4f74d8b < countReadOnlyCountingsButSetable___e8cbcb25bc924f5e91474f39d1dddf9c; i___0bf4b71909d64427bc1d06d4d4f74d8b++)
//            {
//                var @Int16___ba8d138814744f7697db3fe9eb4dcee1 = reader.ReadInt16();
//                @ReadOnlyCountingsButSetable___b2f2e14067b24f648fcdd8a503f56d1d.Add(@Int16___ba8d138814744f7697db3fe9eb4dcee1);
//            }


//            var @Right___51c9587c55044acab9a870d2be4535a6 = (AccessRight)reader.ReadInt32();

//            DEMO.ComplainBase @Complain___b342c32a6cc844c58dd7d09e30504697;
//            {
//                var @Complain___05419e2bca97492b9ef346666e9fd138 = reader.ReadString();

//                var countComplains___5bff023acd1149259927a19efe436aaa = reader.ReadInt32();
//                var @Complains___41bc6277e26e4bcbb2a5e15eb3388387 = new System.Collections.Generic.List<string>(countComplains___5bff023acd1149259927a19efe436aaa);
//                for (int i___9bd7618e79404db5bb672df934fd0e27 = 0; i___9bd7618e79404db5bb672df934fd0e27 < countComplains___5bff023acd1149259927a19efe436aaa; i___9bd7618e79404db5bb672df934fd0e27++)
//                {
//                    var @String___1355e388e7144faea48b1dd4348f945c = reader.ReadString();
//                    @Complains___41bc6277e26e4bcbb2a5e15eb3388387.Add(@String___1355e388e7144faea48b1dd4348f945c);
//                }


//                @Complain___b342c32a6cc844c58dd7d09e30504697 = new DEMO.ComplainBase();

//                @Complain___b342c32a6cc844c58dd7d09e30504697.Complain = Complain___05419e2bca97492b9ef346666e9fd138;
//                @Complain___b342c32a6cc844c58dd7d09e30504697.Complains = Complains___41bc6277e26e4bcbb2a5e15eb3388387;


//            }


//            var @AssignedUser___1e76c80413f34f288a45c7610272faaf = DEMO.SUTMessage.User.DeserializeMe(reader);

//            System.Drawing.Point @Position___8df28725e93c4938941988693a13a1b7;
//            {
//                var @IsEmpty___35c400fc4d8042208d5da9feefa5a23a = reader.ReadBoolean();
//                var @X___23ad3b47fc8f44a3aa28cf59786ec834 = reader.ReadInt32();
//                var @Y___5f645ee520ac480790a87139e0a30769 = reader.ReadInt32();

//                @Position___8df28725e93c4938941988693a13a1b7 = new System.Drawing.Point(X___23ad3b47fc8f44a3aa28cf59786ec834, Y___5f645ee520ac480790a87139e0a30769);



//            }


//            var @ContactUser___c6ba80454882441992f8ed6c79860095 = DEMO.SUTMessage.User.DeserializeMe(reader);
//            var @AlternativUser___5c43d9aa22524dad814c0724db8d6432 = DEMO.SUTMessage.User.DeserializeMe(reader);
//            var @X___e0c2f4d23c3949a8b6331144c5e70152 = reader.ReadInt32();
//            var @countPositions___9e4fef9a15754362bad4c5a93d8e9914 = reader.ReadInt32();

//            var returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809 = new DEMO.SUTMessage();

//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Positions = Positions___af785b88f201418fbb64e6bacb60bb71.ToArray();
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Text = Text___567eb6da2a4a40b8839f32682323aef9;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.UsersList = UsersList___dcee38cccd5640cba39ed970cac813a3;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.ComplainsBases = ComplainsBases___a9ab8aaeaafa43c79abca40cc4d9a8d0;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Countings = Countings___9899105a9d774d349cb8a0c05279b1ac;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.CountingDic = CountingDic___d9b8473c626f4e239c72aed2ff285df1;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.ReadOnlyCountingsButSetable = ReadOnlyCountingsButSetable___b2f2e14067b24f648fcdd8a503f56d1d;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Right = Right___51c9587c55044acab9a870d2be4535a6;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Complain = Complain___b342c32a6cc844c58dd7d09e30504697;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.AssignedUser = AssignedUser___1e76c80413f34f288a45c7610272faaf;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.Position = Position___8df28725e93c4938941988693a13a1b7;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.AlternativUser = AlternativUser___5c43d9aa22524dad814c0724db8d6432;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.X = X___e0c2f4d23c3949a8b6331144c5e70152;
//            returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809.countPositions = countPositions___9e4fef9a15754362bad4c5a93d8e9914;


//            return returnValue___e1e8a3b26d754eb7b8d77b5cbe5f8809;
//        }
//    }
//}