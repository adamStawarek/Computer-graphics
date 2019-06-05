using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;


namespace ImageEditor.ViewModel
{
    
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);        
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<FilterDesignerViewModel>();
            SimpleIoc.Default.Register<CanvasViewModel>();
            SimpleIoc.Default.Register<ClippingViewModel>();
            SimpleIoc.Default.Register<StereoscopyViewModel>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public FilterDesignerViewModel Designer
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FilterDesignerViewModel>();

            }
        }

        public CanvasViewModel Canvas
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CanvasViewModel>();
            }
        }

        public ClippingViewModel Clipping   
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ClippingViewModel>();
            }
        }

        public StereoscopyViewModel Stereoscopy
        {
            get
            {
                return ServiceLocator.Current.GetInstance<StereoscopyViewModel>();
            }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}