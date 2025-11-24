namespace Test.Mcp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GraphMetadata
    {
        public string Description { get; set; } = null;
    }

    public class Person
    {
        public string Name { get; set; } = null;
        public bool IsHandsome { get; set; } = true;
        public int Age { get; set; } = 0;
        public Hobby Hobby { get; set; } = null;
        public override string ToString()
        {
            return "Name: " + Name + " handsome: " + IsHandsome + " age: " + Age;
        }
    }

    public class Hobby
    {
        public string Name { get; set; } = null;
        public int HoursPerWeek { get; set; } = 0;
        public override string ToString()
        {
            return "Name: " + Name + " hours/week: " + HoursPerWeek;
        }
    }

    public class ISP
    {
        public string Name { get; set; } = null;
        public int Mbps { get; set; } = 100;
    }

    public class Internet
    {
        public string Name { get; } = "Internet";
    }

    public class HostingProvider
    {
        public string Name { get; set; } = null;
    }

    public class Application
    {
        public string Name { get; set; } = null;
    }
}

