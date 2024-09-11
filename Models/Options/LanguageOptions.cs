namespace ConectaCartagena.Models.Options
{
    public class LanguageOptions
    {
        public Prompts promts { get; set; }
        public Messages messages { get; set; }
    }

    public class Prompts
    {
        public TouristExpert tourist_expert { get; set; }
    }

    public class TouristExpert
    {
        public string es { get; set; }
        public string en { get; set; }
        public string fr { get; set; }
        public string it { get; set; }
    }

    public class Messages
    {
        public Welcome welcome { get; set; }
    }

    public class Welcome
    {
        public string es { get; set; }
        public string en { get; set; }
        public string fr { get; set; }
        public string it { get; set; }
    }
}
