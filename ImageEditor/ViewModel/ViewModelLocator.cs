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
     
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}