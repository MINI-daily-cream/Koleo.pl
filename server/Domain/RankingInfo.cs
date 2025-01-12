﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class RankingInfo
    {
        public string Ranking_Id { get; set; }
        public string User_Id { get; set; }
        
        public string RankingName { get; set; }

        public string Position { get; set; }

        public string Points { get; set; }

        public string Username { get; set; }

        public RankingInfo(string _Ranking_Id, string _User_Id, string _Ranking_name, string _Position, string _Points, string _Username)
        {
            Ranking_Id = _Ranking_Id;
            User_Id = _User_Id; 
            RankingName= _Ranking_name;
            Position = _Position;
            Points = _Points;
            Username = _Username;
        }

    }
}
