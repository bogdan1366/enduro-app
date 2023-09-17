using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnduroApp
{
    [Serializable()]
    public class Database
    {

        public volatile int NrofCheckpoints = 10;
        public volatile int compNr = 1;
        
        public List<Riders> AllRiders = new List<Riders>();
        public List<Riders> Feminin_15_18 = new List<Riders>();
        public List<Riders> Feminin_19_plus = new List<Riders>();
        public List<Riders> Masculin_15_18 = new List<Riders>();
        public List<Riders> Masculin_19_29 = new List<Riders>();
        public List<Riders> Masculin_30_39 = new List<Riders>();
        public List<Riders> Masculin_40_plus = new List<Riders>();
        public List<Riders> Hobby = new List<Riders>();

        public Database() { }
                
    }
}
