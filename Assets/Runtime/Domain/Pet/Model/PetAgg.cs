namespace Domain
{
    /// <summary>
    /// 单只宠物
    /// </summary>
    public class PetAgg
    {
        /// <summary> 宠物唯一ID </summary>
        public long no { get; private set;}
        public void SetNo(long id) => no = id;
        
        public PetInfo_Entity PetInfo_E { get; private set; }
        public PetProperty_Entity PetProperty_E{ get; private set; }

        public PetAgg(int no)
        {
            this.no = no;
            PetInfo_E = new PetInfo_Entity();
            PetProperty_E = new PetProperty_Entity();
        }
    }

    public class PetInfo_Entity
    {
        /// <summary> 昵称 </summary>
        public string Nickname { get; private set; }
        /// <summary> 抓捕次数 </summary>
        public int CatchTime { get; private set;}
        /// <summary> 是否被发现 </summary>
        public bool Befind {get; private set;}
        /// <summary> 席位 </summary>
        public int SeatIndex { get; private set; }
        /// <summary> 点赞 </summary>
        public bool Praised{ get; private set; }
        
        /// <summary> 星级 </summary>
        public int StarLevel{ get; private set; }
        
        public void SetNickname (string name) => Nickname = name;
        public void SetSeatIndex (int index) => SeatIndex = index;
        public void SetCatchTime (int time) => CatchTime = time;
        public void SetBefind (bool befind) => Befind = befind;
        public void SetPraised(bool praised) => Praised = praised;
        public void SetStarLevel (int starLevel) => StarLevel = starLevel;
        
        public PetInfo_Entity()
        {
            Befind = false;
        }
    }

    public class PetProperty_Entity 
    {
        /// <summary> 当前攻击 </summary>
        public int Attack { get; private set; }
        public int Speed{ get; private set; }
        public int Health{ get; private set; }
        public int Defend{ get; private set; }

        public void SetProperty(int atk, int speed, int health, int defend)
        {
            Attack = atk;
            Speed = speed;
            Health = health;
            Defend = defend;
        }
    }
}