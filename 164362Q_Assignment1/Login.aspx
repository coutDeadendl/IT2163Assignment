<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="_164362Q_Assignment1.Login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>SITConnect - Login</title>
    <script src="https://www.google.com/recaptcha/api.js?render=6LenlfwZAAAAAErbOxpZHMSCOUHSgoUTYUdqWnWc"></script>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@4.5.3/dist/css/bootstrap.min.css" integrity="sha384-TX8t27EcRE3e/ihU7zmQxVncDAy5uIKz4rEkgIXeMed4M0jlfIDPvg6uqKI2xXr2" crossorigin="anonymous"/>    
</head>
<body>
    <script>
        grecaptcha.ready(function () {
            grecaptcha.execute('6LenlfwZAAAAAErbOxpZHMSCOUHSgoUTYUdqWnWc', { action: 'Register' }).then(function (token) {
                document.getElementById("g-recaptcha-response").value = token;
            });
        });
    </script>
    <div>
        <center><p class="h1">SITConnect!</p></center>
        <asp:Panel ID="regSuccess" runat="server" visible="false">
            <div class="alert alert-success m-auto p-3" style="width:90%;" role="alert">Registration success!</div>
        </asp:Panel>
        <asp:Panel ID="failure" runat="server" visible="false">
            <div class="alert alert-danger m-auto p-3" style="width:90%;" role="alert">Please log in again.</div>
        </asp:Panel>
        <form id="form1" runat="server">
            <fieldset>
                <legend class="font-weight-bold">Login</legend>
                <table cellpadding="3">
                    <tr>
                        <td><asp:Label ID="lb_email" runat="server" Text="Email Address"></asp:Label></td>
                        <td><asp:TextBox ID="tb_email" runat="server"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Label ID="lb_password" runat="server" Text="Password"></asp:Label></td>
                        <td><asp:TextBox ID="tb_password" runat="server" TextMode="Password"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td><asp:Button ID="btn_login" runat="server" class="btn btn-outline-dark" Text="Login" OnClick="btn_login_Click" />
                        </td>
                        <td>
                            <input type="hidden" id="g-recaptcha-response" name="g-recaptcha-response"/>
                            <asp:Label ID="lb_warning" runat="server" Visible="false"></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td><a href="/Registration.aspx">Register instead?</a></td>
                    </tr>
                </table>
            </fieldset>
        </form>
    </div>
</body>
</html>