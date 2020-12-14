namespace Explorer.ViewModels
{
    public abstract class FileEntityViewModel : BaseViewModel
    {
        protected FileEntityViewModel(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
        public string FullName { get; set; }
    }
}