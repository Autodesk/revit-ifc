using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Revit.IFC.Common.Utility;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for COBieCompanyInfoTab.xaml
   /// </summary>
   public partial class COBieCompanyInfoTab : UserControl
   {
      COBieCompanyInfo companyInfo;
      public string CompanyInfoStr {
         get
         {
            PopulateCompanyInfo();
            return companyInfo.ToJsonString();
         }
      }

      public COBieCompanyInfoTab(string compInfoStr)
      {
         InitializeComponent();
         textBox_CompanyPhoneValidation.Visibility = Visibility.Hidden;
         textBox_CompanyEmailValidation.Visibility = Visibility.Hidden;
         textBox_CompanyName.Focus();
         textBox_CompanyType.Text = "example: [Company Type Classification Name]<replace here with code>";

         Autodesk.Revit.DB.ProjectInfo rvtProjectInfo = IFCCommandOverrideApplication.TheDocument.ProjectInformation;

         if (!string.IsNullOrEmpty(compInfoStr))
         {
            companyInfo = new COBieCompanyInfo(compInfoStr);
         }
         else
            companyInfo = new COBieCompanyInfo();

         if (string.IsNullOrEmpty(companyInfo.CompanyName))
            companyInfo.CompanyName = rvtProjectInfo.OrganizationName;

         PopulatePage();
      }

      private void button_TypeSource_Click(object sender, RoutedEventArgs e)
      {

      }

      private void textBox_TypeSource_TextChanged(object sender, TextChangedEventArgs e)
      {
      }

      bool companyTypeVisited = false;
      private void textBox_CompanyType_GotFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         tb.Text = string.Empty;
         tb.GotFocus -= textBox_CompanyType_GotFocus;
         tb.FontStyle = FontStyles.Normal;
         tb.FontWeight = FontWeights.Normal;
         companyTypeVisited = true;
      }

      bool companyPhoneVisited = false;
      private void textBox_CompanyPhone_GotFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         tb.Text = string.Empty;
         tb.GotFocus -= textBox_CompanyPhone_GotFocus;
         tb.FontStyle = FontStyles.Normal;
         tb.FontWeight = FontWeights.Normal;
         companyPhoneVisited = true;
      }

      bool companyEmailVisited = false;
      private void textBox_CompanyEmail_GotFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         tb.Text = string.Empty;
         tb.GotFocus -= textBox_CompanyEmail_GotFocus;
         tb.FontStyle = FontStyles.Normal;
         tb.FontWeight = FontWeights.Normal;
         companyEmailVisited = true;  
      }

      private void textBox_CompanyPhone_LostFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         if (string.IsNullOrEmpty(tb.Text))
            return;
         // Validate phone number format using regular expression
         companyInfo.CompanyPhone = tb.Text;
         ValidateCompanyPhone();
      }

      private void ValidateCompanyPhone()
      {
         if (!companyInfo.PhoneValidator())
         {
            textBox_CompanyPhoneValidation.Visibility = Visibility.Visible;
            textBox_CompanyPhoneValidation.Text = "!";
            textBox_CompanyPhoneValidation.FontWeight = FontWeights.ExtraBold;
            textBox_CompanyPhoneValidation.FontSize = 14;
            textBox_CompanyPhone.ToolTip = "Phone number is not in a valid format!";
            textBox_CompanyPhone.Background = Brushes.Red;
         }
         else
         {
            textBox_CompanyPhoneValidation.Visibility = Visibility.Hidden;
            textBox_CompanyPhoneValidation.Text = "";
            textBox_CompanyPhone.ToolTip = "";
            textBox_CompanyPhone.Background = null;
         }
      }

      private void textBox_CompanyEmail_LostFocus(object sender, RoutedEventArgs e)
      {
         TextBox tb = sender as TextBox;
         if (string.IsNullOrEmpty(tb.Text))
            return;
         // Validate email format using regular expression
         companyInfo.CompanyEmail = tb.Text;
         ValidateCompanyEmail();
      }

      private void ValidateCompanyEmail()
      {
         if (!companyInfo.EmailValidator())
         {
            textBox_CompanyEmailValidation.Visibility = Visibility.Visible;
            textBox_CompanyEmailValidation.Text = "!";
            textBox_CompanyEmailValidation.FontWeight = FontWeights.ExtraBold;
            textBox_CompanyEmailValidation.FontSize = 14;
            textBox_CompanyEmailValidation.ToolTip = "Email address is not in a valid format!";
            textBox_CompanyEmail.Background = Brushes.Red;
         }
         else
         {
            textBox_CompanyEmailValidation.Visibility = Visibility.Hidden;
            textBox_CompanyEmailValidation.Text = "";
            textBox_CompanyEmail.ToolTip = "";
            textBox_CompanyEmail.Background = null;
         }
      }

      private void PopulateCompanyInfo()
      {
         companyInfo.CompanyType = companyTypeVisited? textBox_CompanyType.Text : "";
         companyInfo.CompanyName = textBox_CompanyName.Text;
         companyInfo.StreetAddress = textBox_StreetAddress.Text;
         companyInfo.City = textBox_City.Text;
         companyInfo.State_Region = textBox_State.Text;
         companyInfo.PostalCode = textBox_PostalCode.Text;
         companyInfo.Country = textBox_Country.Text;
         companyInfo.CompanyPhone = companyPhoneVisited? textBox_CompanyPhone.Text : "";
         companyInfo.CompanyEmail = companyEmailVisited? textBox_CompanyEmail.Text : "";
      }

      private void PopulatePage()
      {
         if (!string.IsNullOrEmpty(companyInfo.CompanyType))
         {
            textBox_CompanyType.Text = companyInfo.CompanyType;
            textBox_CompanyType.GotFocus -= textBox_CompanyType_GotFocus;
            textBox_CompanyType.FontStyle = FontStyles.Normal;
            textBox_CompanyType.FontWeight = FontWeights.Normal;
            companyTypeVisited = true;
         }
         textBox_CompanyName.Text = companyInfo.CompanyName;
         textBox_StreetAddress.Text = companyInfo.StreetAddress;
         textBox_City.Text = companyInfo.City;
         textBox_State.Text = companyInfo.State_Region;
         textBox_PostalCode.Text = companyInfo.PostalCode;
         textBox_Country.Text = companyInfo.Country;
         if (!string.IsNullOrEmpty(companyInfo.CompanyPhone))
         {
            textBox_CompanyPhone.Text = companyInfo.CompanyPhone;
            textBox_CompanyPhone.GotFocus -= textBox_CompanyPhone_GotFocus;
            textBox_CompanyPhone.FontStyle = FontStyles.Normal;
            textBox_CompanyPhone.FontWeight = FontWeights.Normal;
            companyPhoneVisited = true;
            ValidateCompanyPhone();
         }
         if (!string.IsNullOrEmpty(companyInfo.CompanyEmail))
         {
            textBox_CompanyEmail.Text = companyInfo.CompanyEmail;
            textBox_CompanyEmail.GotFocus -= textBox_CompanyEmail_GotFocus;
            textBox_CompanyEmail.FontStyle = FontStyles.Normal;
            textBox_CompanyEmail.FontWeight = FontWeights.Normal;
            companyEmailVisited = true;
            ValidateCompanyEmail();
         }
      }
   }
}
