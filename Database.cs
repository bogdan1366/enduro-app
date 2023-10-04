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
        public List<Riders> Master_Women_35_plus = new List<Riders>();
        public List<Riders> Women_21_34 = new List<Riders>();
        public List<Riders> U21_Women = new List<Riders>();
        public List<Riders> U21_Men = new List<Riders>();
        public List<Riders> Men_21_39 = new List<Riders>();
        public List<Riders> Master_Men_40_plus = new List<Riders>();
        public List<Riders> U15_Juniors = new List<Riders>();

        public Database() { }
                
    }
}
