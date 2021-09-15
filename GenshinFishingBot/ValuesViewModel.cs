namespace GenshinFishingBot
{
    public class ValuesViewModel : ViewModel
    {
        private int _x;
        public int X
        {
            get => _x;
            set
            {
                if (_x == value)
                    return;
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }
        private int _y;
        public int Y
        {
            get => _y;
            set
            {
                if (_y == value)
                    return;
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }
        private int _width;
        public int Width
        {
            get => _width;
            set
            {
                if (_width == value)
                    return;
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
        private int _height;
        public int Height
        {
            get => _height;
            set
            {
                if (_height == value)
                    return;
                _height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
        private string _perfText;
        public string PerfText
        {
            get => _perfText;
            set
            {
                if (_perfText == value)
                    return;
                _perfText = value;
                OnPropertyChanged(nameof(PerfText));
            }
        }
    }
}
