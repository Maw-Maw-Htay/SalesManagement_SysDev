using System.Windows;

namespace SalesManagement_SysDev;

public partial class F_login : Window
{
    public F_login()
    {
        InitializeComponent();
        Loaded += (_, _) => PrepareDatabase();
    }

    private void PrepareDatabase()
    {
        try
        {
            DatabaseInitializer.EnsureReady();
            lblMessage.Text = "データベース接続済み。社員IDとパスワードを入力してください。";
            lblMessage.Foreground = System.Windows.Media.Brushes.DarkGreen;
        }
        catch (Exception ex)
        {
            lblMessage.Text = $"データベースに接続できません。\n{ex.Message}";
            lblMessage.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtEmployeeId.Text.Trim(), out var id))
        {
            lblMessage.Text = "社員IDを数字で入力してください。";
            return;
        }

        try
        {
            var employee = BusinessService.Login(id, txtPassword.Password);
            if (employee == null)
            {
                lblMessage.Text = "社員IDまたはパスワードが正しくありません。";
                return;
            }

            AppSession.Employee = employee;
            new F_Main().Show();
            Close();
        }
        catch (Exception ex)
        {
            lblMessage.Text = $"ログイン処理に失敗しました。\n{ex.Message}";
        }
    }

    private void btn_CleateDabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DatabaseInitializer.EnsureReady();
            MessageBox.Show("SalesManagement データベースの準備が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "データベースエラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btn_InsertSampleData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DatabaseInitializer.Seed150SampleData();
            MessageBox.Show("全業務テーブルへ150件のサンプル系列を登録しました。\n既存データは重複登録されません。\n\n管理者: 1227 / 1227\n営業: 310 / 0310\n物流: 116 / 0116", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "サンプルデータエラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
