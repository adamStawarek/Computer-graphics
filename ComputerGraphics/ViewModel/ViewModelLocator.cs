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
     
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}