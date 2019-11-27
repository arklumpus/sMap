using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace sMap_GUI
{
    public class PiMenuIcon : UserControl
    {
        public enum IconTypes
        {
            Fix, Dirichlet, Equal, Multinomial, ML
        }

        private IconTypes _iconType = IconTypes.Fix;

        public IconTypes IconType
        {
            get
            {
                return _iconType;
            }

            set
            {
                _iconType = value;
                switch (_iconType)
                {
                    case IconTypes.Fix:
                        this.FindControl<Canvas>("FixCanvas").IsVisible = true;
                        this.FindControl<Canvas>("DirichletCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MultinomialCanvas").IsVisible = false;
                        this.FindControl<Canvas>("EqualCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MLCanvas").IsVisible = false;
                        break;
                    case IconTypes.Dirichlet:
                        this.FindControl<Canvas>("FixCanvas").IsVisible = false;
                        this.FindControl<Canvas>("DirichletCanvas").IsVisible = true;
                        this.FindControl<Canvas>("MultinomialCanvas").IsVisible = false;
                        this.FindControl<Canvas>("EqualCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MLCanvas").IsVisible = false;
                        break;
                    case IconTypes.Multinomial:
                        this.FindControl<Canvas>("FixCanvas").IsVisible = false;
                        this.FindControl<Canvas>("DirichletCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MultinomialCanvas").IsVisible = true;
                        this.FindControl<Canvas>("EqualCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MLCanvas").IsVisible = false;
                        break;
                    case IconTypes.Equal:
                        this.FindControl<Canvas>("FixCanvas").IsVisible = false;
                        this.FindControl<Canvas>("DirichletCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MultinomialCanvas").IsVisible = false;
                        this.FindControl<Canvas>("EqualCanvas").IsVisible = true;
                        this.FindControl<Canvas>("MLCanvas").IsVisible = false;
                        break;
                    case IconTypes.ML:
                        this.FindControl<Canvas>("FixCanvas").IsVisible = false;
                        this.FindControl<Canvas>("DirichletCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MultinomialCanvas").IsVisible = false;
                        this.FindControl<Canvas>("EqualCanvas").IsVisible = false;
                        this.FindControl<Canvas>("MLCanvas").IsVisible = true;
                        break;
                }
            }
        }

        public PiMenuIcon()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
