﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <%--<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>--%>
    <%--<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_AjaxValidation.js")%>" type="text/javascript"></script>--%>
    <%--<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_PopupPanel.js")%>" type="text/javascript"></script>--%>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_DragAndDrop.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Autocomplete.js")%>" type="text/javascript"></script>
    <%--<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_SearchEngine.js")%>" type="text/javascript"></script>--%>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
    <div class="grid_16">
     <input type="hidden" id="<%=ViewDataKeys.TabId%>" name="<%=ViewDataKeys.TabId%>" value="<%=(string)ViewData[ViewDataKeys.TabId]%>" />

    <h2>
        <span class="typeNiceName"><%= ViewData[ViewDataKeys.EntityTypeNiceName]%></span>
        <span class="title"><%= ViewData[ViewDataKeys.PageTitle] ?? "" %></span>
     </h2>
     <div class="operations">
        <%if (Model != null && Navigator.Manager.ShowOkSave(Model.GetType(), false)){ %>
            <div id="btnSave" class="OperationDiv" onclick="javascript:TrySave({});">Guardar</div>   
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), "") %>     
     </div>
     <div class="clearall"></div>
     <div class="validationSummaryAjax">
        <%= Html.ValidationSummaryAjax() %>     
     </div>    
    <div id="divMainControl" class="divMainControl">
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    </div>
    
    </div>
   <div class="clear"></div>
   <div id="divASustituir"></div>
 <%}%>
    
</asp:Content>
