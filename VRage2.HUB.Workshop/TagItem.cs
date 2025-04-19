namespace VRage2.HUB.Workshop
{
    public class TagItem
    {
        public string Name { get; }
        public bool IsSelected { get; set; }

        public TagItem(string name) => Name = name;
    }
}
