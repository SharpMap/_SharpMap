<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Simple.aspx.cs" Inherits="Simple" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
   <title>Simple map</title>
</head>
<body>
<h2>Simple map</h2>
<form runat="server">
    <div>   
    	<asp:RadioButtonList ID="rblMapTools" runat="server" RepeatDirection="Horizontal">
            <asp:ListItem Value="0">Zoom in</asp:ListItem>
            <asp:ListItem Value="1">Zoom out</asp:ListItem>
            <asp:ListItem Value="2" Selected="True">Pan</asp:ListItem>
        </asp:RadioButtonList>
        <asp:ImageButton Width="600" Height="300" ID="imgMap" runat="server" OnClick="imgMap_Click" style="border: 1px solid #000;" />
    </div>
</form>

</body>
</html>
