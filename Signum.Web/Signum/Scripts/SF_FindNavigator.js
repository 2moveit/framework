﻿"use strict";

SF.registerModule("FindNavigator", function () {

    (function ($) {
        $.widget("SF.findNavigator", {

            options: {
                allowChangeColumns: true,
                allowMultiple: null,
                columnMode: "Add",
                columns: null, //List of column names "token1,displayName1;token2,displayName2"
                create: true,
                elems: null,
                entityContextMenu: true,
                filterMode: "Visible",
                filters: null, //List of filter names "token1,operation1,value1;token2,operation2,value2"
                openFinderUrl: null,
                onCancelled: null,
                onOk: null,
                onOkClosed: null,
                orders: [], //A Json array like ["Id","-Name"] => Id asc, then Name desc
                prefix: "",
                searchOnLoad: false,
                view: true,
                webQueryName: null
            },

            keys: {
                elems: "sfElems",
                page: "sfPage"
            },

            pf: function (s) {
                return "#" + SF.compose(this.options.prefix, s);
            },

            tempDivId: function () {
                return SF.compose(this.options.prefix, "Temp");
            },

            _create: function () {
                var self = this;

                var closeMyOpenedCtxMenu = function (target) {
                    if (self.element.find(".sf-search-ctxmenu-overlay").length > 0) {
                        $('.sf-search-ctxmenu-overlay').remove();
                        return false;
                    }
                    return true;
                };

                var $tblResults = self.element.find(".sf-search-results-container");
                $tblResults.on("click", "th:not(.th-col-entity):not(.th-col-selection),th:not(.th-col-entity):not(.th-col-selection) span,th:not(.th-col-entity):not(.th-col-selection) .sf-header-droppable", function (e) {
                    if (e.target != this) {
                        return;
                    }
                    self.newSortOrder($(e.target).closest("th"), e.shiftKey);
                    self.search();
                    return false;
                });

                $tblResults.on("contextmenu", "th:not(.th-col-entity):not(.th-col-selection)", function (e) {
                    if (!closeMyOpenedCtxMenu(e.target)) {
                        return false;
                    }
                    self.headerContextMenu(e);
                    return false;
                });

                $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {
                    if (!closeMyOpenedCtxMenu(e.target)) {
                        return false;
                    }

                    var $this = $(this).closest("td");
                    var index = $this.index();
                    var $th = $this.closest("table").find("th").eq(index);
                    if ($th.hasClass('th-col-selection')) {
                        return false;
                    }
                    if ($th.hasClass('th-col-entity')) {
                        if (self.options.entityContextMenu == true) {
                            self.entityContextMenu(e);
                        }
                    }
                    else {
                        self.cellContextMenu(e);
                    }
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.quickfilter", function () {
                    var $elem = $(this).closest("td");
                    $('.sf-search-ctxmenu-overlay').remove();
                    self.quickFilterCell($elem);
                });

                $tblResults.on("click", ".sf-search-ctxitem.quickfilter-header", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();
                    self.quickFilterHeader($elem);
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.remove-column", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();

                    self.removeColumn($elem);
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.edit-column", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();

                    self.editColumn($elem);
                    return false;
                });

                $tblResults.on("click", ".sf-pagination-button", function () {
                    $(self.pf(self.keys.page)).val($(this).attr("data-page"));
                    self.search();
                });

                $tblResults.on("change", ".sf-pagination-size", function () {
                    if ($(this).find("option:selected").val() == -1) {
                        self.clearResults();
                    }
                    else {
                        self.search();
                    }
                });

                $(this.pf("sfFullScreen")).on("mousedown", function (e) {
                    e.preventDefault();
                    self.fullScreen(e);
                });

                this.createMoveColumnDragDrop();

                $tblResults.on("selectstart", "th:not(.th-col-entity):not(.th-col-selection)", function (e) {
                    return false;
                });

                this.element.on("sf-new-subtokens-combo", function (event, idSelectedCombo) {
                    self.newSubTokensComboAdded($("#" + idSelectedCombo));
                });

                if (this.options.searchOnLoad) {
                    this.searchOnLoad();
                }
            },

            createCtxMenu: function ($rightClickTarget) {
                var left = $rightClickTarget.position().left + ($rightClickTarget.outerWidth() / 2);
                var top = $rightClickTarget.position().top + ($rightClickTarget.outerHeight() / 2);

                var $cmenu = $("<div class='sf-search-ctxmenu'></div>");
                $cmenu.css({
                    left: left,
                    top: top,
                    zIndex: '101'
                });

                var $ctxMenuOverlay = $('<div class="sf-search-ctxmenu-overlay"></div>').click(function (e) {
                    SF.log("contextmenu click");
                    var $clickTarget = $(e.target);
                    if ($clickTarget.hasClass("sf-search-ctxitem") || $clickTarget.parent().hasClass("sf-search-ctxitem"))
                        $cmenu.hide();
                    else
                        $('.sf-search-ctxmenu-overlay').remove();
                }).append($cmenu);

                return $ctxMenuOverlay;
            },

            headerContextMenu: function (e) {
                var $th = $(e.target).closest("th");
                var $menu = this.createCtxMenu($th);

                var $itemContainer = $menu.find(".sf-search-ctxmenu");
                $itemContainer.append("<div class='sf-search-ctxitem quickfilter-header'>" + lang.signum.addFilter + "</div>");

                if (this.options.allowChangeColumns) {
                    $itemContainer.append("<div class='sf-search-ctxitem edit-column'>" + lang.signum.editColumnName + "</div>")
                        .append("<div class='sf-search-ctxitem remove-column'>" + lang.signum.removeColumn + "</div>");
                }

                $th.append($menu);
                return false;
            },

            cellContextMenu: function (e) {
                var $td = $(e.target);
                var $menu = this.createCtxMenu($td);

                $menu.find(".sf-search-ctxmenu")
                    .html("<div class='sf-search-ctxitem quickfilter'>" + lang.signum.addFilter + "</div>");

                $td.append($menu);
                return false;
            },

            entityContextMenu: function (e) {
                var $td = $(e.target).closest("td");
                $td.addClass("sf-ctxmenu-active");

                var $menu = this.createCtxMenu($td);
                var $itemContainer = $menu.find(".sf-search-ctxmenu");

                var requestData = {
                    lite: $td.parent().data('entity'),
                    webQueryName: this.options.webQueryName,
                    prefix: this.options.prefix
                };

                $.ajax({
                    url: SF.Urls.entityContextMenu,
                    data: requestData,
                    success: function (items) {
                        $itemContainer.html(items);
                        $td.append($menu);
                        SF.triggerNewContent($menu);
                    }
                });

                return false;
            },

            fullScreen: function (evt) {
                var url = this.element.attr("data-find-url") + this.requestDataForSearchInUrl();
                if (evt.ctrlKey || evt.which == 2) {
                    window.open(url);
                }
                else if (evt.which == 1) {
                    window.location.href = url;
                }
            },

            search: function () {
                var $searchButton = $(this.pf("qbSearch"));
                $searchButton.addClass("sf-searching");
                var self = this;
                $.ajax({
                    url: SF.Urls.search,
                    data: this.requestDataForSearch(),
                    success: function (r) {
                        var $tbody = self.element.find(".sf-search-results-container tbody");
                        if (!SF.isEmpty(r)) {
                            $tbody.html(r);
                            SF.triggerNewContent(self.element.find(".sf-search-results-container tbody"));
                        }
                        else {
                            $tbody.html("");
                        }
                        $searchButton.removeClass("sf-searching");
                    }
                });
            },

            requestDataForSearch: function () {
                var requestData = new Object();
                requestData["webQueryName"] = this.options.webQueryName;
                requestData["elems"] = $(this.pf(this.keys.elems)).val();
                requestData["page"] = $(this.pf(this.keys.page)).val();
                requestData["allowMultiple"] = this.options.allowMultiple;
                requestData["view"] = this.options.view;
                requestData["filters"] = this.serializeFilters();
                requestData["filterMode"] = this.options.filterMode;
                requestData["orders"] = this.serializeOrders();
                requestData["columns"] = this.serializeColumns();
                requestData["columnMode"] = 'Replace';

                requestData["prefix"] = this.options.prefix;
                return requestData;
            },

            requestDataForSearchInUrl: function () {
                return "?elems=" + $(this.pf(this.keys.elems)).val() +
                    "&page=" + $(this.pf(this.keys.page)).val() +
                    "&allowMultiple=" + this.options.allowMultiple +
                    "&filters=" + this.serializeFilters() +
                    "&filterMode=Visible" +
                    "&orders=" + this.serializeOrders() +
                    "&columns=" + this.serializeColumns() +
                    "&columnMode=Replace" +
                    "&view=" + this.options.view;
            },

            serializeFilters: function () {
                var result = "", self = this;
                $(this.pf("tblFilters > tbody > tr")).each(function () {
                    result += self.serializeFilter($(this)) + ";";
                });
                return result;
            },

            serializeFilter: function ($filter) {
                var id = $filter[0].id;
                var index = id.substring(id.lastIndexOf("_") + 1, id.length);

                var selector = $(SF.compose(this.pf("ddlSelector"), index) + " option:selected", $filter);
                var value = $(SF.compose(this.pf("value"), index), $filter).val();

                var valBool = $("input:checkbox[id=" + SF.compose(SF.compose(this.options.prefix, "value"), index) + "]", $filter); //it's a checkbox
                if (valBool.length > 0) {
                    value = valBool[0].checked;
                }
                else {
                    var info = new SF.RuntimeInfo(SF.compose(SF.compose(this.options.prefix, "value"), index));
                    if (info.find().length > 0) { //If it's a Lite, the value is the Id
                        value = info.runtimeType() + ";" + info.id();
                        if (value == ";") {
                            value = "";
                        }
                    }

                    //Encode value CSV-ish style
                    var hasQuote = value.indexOf("\"") != -1;
                    if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
                        if (hasQuote) {
                            value = value.replace(/"/g, "\"\"");
                        }
                        value = "\"" + value + "\"";
                    }
                }

                return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
            },

            serializeOrders: function () {
                return SF.FindNavigator.serializeOrders(this.options.orders);
            },

            serializeColumns: function () {
                var result = "";
                var self = this;
                $(this.pf("tblResults thead tr th:not(.th-col-entity):not(.th-col-selection)")).each(function () {
                    var $this = $(this);
                    var token = $this.find("input:hidden").val();
                    var displayName = $this.text().trim();
                    if (token == displayName) {
                        result += token;
                    }
                    else {
                        result += token + "," + displayName;
                    }
                    result += ";";
                });
                return result;
            },

            onSearchOk: function () {
                var self = this;
                this.hasSelectedItems(function (items) {
                    var doDefault = (self.options.onOk != null) ? self.options.onOk(items) : true;
                    if (doDefault != false) {
                        $('#' + self.tempDivId()).remove();
                        if (self.options.onOkClosed != null) {
                            self.options.onOkClosed();
                        }
                    }
                });
            },

            onSearchCancel: function () {
                $('#' + this.tempDivId()).remove();
                if (this.options.onCancelled != null) {
                    this.options.onCancelled();
                }
            },

            hasSelectedItems: function (onSuccess) {
                var items = this.selectedItems();
                if (items.length == 0) {
                    SF.Notify.info(lang.signum.noElementsSelected);
                    return;
                }
                onSuccess(items);
            },

            selectedItems: function () {
                var items = [];
                var selected = $("input:radio[name=" + SF.compose(this.options.prefix, "rowSelection") + "]:checked, input:checkbox[name^=" + SF.compose(this.options.prefix, "rowSelection") + "]:checked");
                if (selected.length == 0)
                    return items;

                var self = this;
                selected.each(function (i, v) {
                    var parts = v.value.split("__");
                    var item = {
                        id: parts[0],
                        type: parts[1],
                        toStr: parts[2],
                        link: $(this).parent().next().children('a').attr('href')
                    };
                    items.push(item);
                });

                return items;
            },

            splitSelectedIds: function () {
                SF.log("FindNavigator splitSelectedIds");
                var selected = this.selectedItems();
                var result = [];
                for (var i = 0, l = selected.length; i < l; i++) {
                    result.push(selected[i].id + ",");
                }

                if (result.length) {
                    var result2 = result.join('');
                    return result2.substring(0, result2.length - 1);
                }
                return '';
            },

            newSortOrder: function ($th, multiCol) {
                var columnName = $th.find("input:hidden").val();
                var currentOrders = this.options.orders;

                var indexCurrOrder = $.inArray(columnName, currentOrders);
                var newOrder = "";
                if (indexCurrOrder === -1) {
                    indexCurrOrder = $.inArray("-" + columnName, currentOrders);
                }
                else {
                    newOrder = "-";
                }

                if (!multiCol) {
                    this.element.find(".sf-search-results-container th").removeClass("sf-header-sort-up sf-header-sort-down");
                    this.options.orders = [newOrder + columnName];
                }
                else {
                    if (indexCurrOrder !== -1) {
                        this.options.orders[indexCurrOrder] = newOrder + columnName;
                    }
                    else {
                        this.options.orders.push(newOrder + columnName);
                    }
                }

                if (newOrder == "-")
                    $th.removeClass("sf-header-sort-down").addClass("sf-header-sort-up");
                else
                    $th.removeClass("sf-header-sort-up").addClass("sf-header-sort-down");
            },

            addColumn: function () {
                if (!this.options.allowChangeColumns || $(this.pf("tblFilters tbody")).length == 0) {
                    throw "Adding columns is not allowed";
                }

                var tokenName = SF.FindNavigator.constructTokenName(this.options.prefix);
                if (SF.isEmpty(tokenName)) {
                    return;
                }

                var prefixedTokenName = SF.compose(this.options.prefix, tokenName);
                if ($(this.pf("tblResults thead tr th[id=\"" + prefixedTokenName + "\"]")).length > 0) {
                    return;
                }

                var $tblHeaders = $(this.pf("tblResults thead tr"));

                var self = this;
                $.ajax({
                    url: $(this.pf("btnAddColumn")).attr("data-url"),
                    data: { "webQueryName": this.options.webQueryName, "tokenName": tokenName },
                    async: false,
                    success: function (columnNiceName) {
                        $tblHeaders.append("<th class='ui-state-default'>" +
                            "<div class='sf-header-droppable sf-header-droppable-right'></div>" +
                            "<div class='sf-header-droppable sf-header-droppable-left'></div>" +
                            "<input type=\"hidden\" value=\"" + tokenName + "\" />" +
                            "<span>" + columnNiceName + "</span></th>");
                        var $newTh = $tblHeaders.find("th:last");
                        self.createMoveColumnDragDrop($newTh, $newTh.find(".sf-header-droppable"));
                    }
                });
            },

            editColumn: function ($th) {
                var colName = $th.text().trim();

                var popupPrefix = SF.compose(this.options.prefix, "newName");

                var divId = "columnNewName";
                var $div = $("<div id='" + divId + "'></div>");
                $div.html("<p>" + lang.signum.enterTheNewColumnName + "</p>")
                    .append("<br />")
                    .append("<input type='text' value='" + colName + "' />")
                    .append("<br />").append("<br />")
                    .append("<input type='button' id='" + SF.compose(popupPrefix, "btnOk") + "' class='sf-button sf-ok-button' value='OK' />");

                var $tempContainer = $("<div></div>").append($div);

                new SF.ViewNavigator({
                    onOk: function () { $th.find("span").html($("#columnNewName > input:text").val()); },
                    prefix: popupPrefix
                }).showViewOk($tempContainer.html());
            },

            moveColumn: function ($source, $target, before) {
                if (before) {
                    $target.before($source);
                }
                else {
                    $target.after($source);
                }

                $source.removeAttr("style"); //remove absolute positioning
                this.clearResults();
                this.createMoveColumnDragDrop();
            },

            createMoveColumnDragDrop: function ($draggables, $droppables) {
                $draggables = $draggables || $(this.pf("tblResults") + " th:not(.th-col-entity):not(.th-col-selection)");
                $droppables = $droppables || $(this.pf("tblResults") + " .sf-header-droppable");

                $draggables.draggable({
                    revert: "invalid",
                    axis: "x",
                    opacity: 0.5,
                    distance: 8,
                    cursor: "move"
                });
                $draggables.removeAttr("style"); //remove relative positioning

                var self = this;
                $droppables.droppable({
                    hoverClass: "sf-header-droppable-active",
                    tolerance: "pointer",
                    drop: function (event, ui) {
                        var $dragged = ui.draggable;

                        var $targetPlaceholder = $(this); //droppable
                        var $targetCol = $targetPlaceholder.closest("th");

                        self.moveColumn($dragged, $targetCol, $targetPlaceholder.hasClass("sf-header-droppable-left"));
                    }
                });
            },

            removeColumn: function ($th) {
                $th.remove();
                this.clearResults();
            },

            clearResults: function () {
                var $tbody = $(this.pf("tblResults tbody"));
                $tbody.find("tr:not('.sf-search-footer')").remove();
                $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
            },

            toggleFilters: function () {
                var $toggler = this.element.find(".sf-filters-header");
                this.element.find(".sf-filters").toggle();
                $toggler.toggleClass('close');
                if ($toggler.hasClass('close')) {
                    $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-n").addClass("ui-icon-triangle-1-e");
                    $toggler.find(".ui-button-text").html(lang.signum.showFilters);
                }
                else {
                    $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-e").addClass("ui-icon-triangle-1-n");
                    $toggler.find(".ui-button-text").html(lang.signum.hideFilters);
                }
                return false;
            },

            addFilter: function (url, requestExtraJsonData) {
                var tableFilters = $(this.pf("tblFilters tbody"));
                if (tableFilters.length == 0) {
                    throw "Adding filters is not allowed";
                }

                var tokenName = SF.FindNavigator.constructTokenName(this.options.prefix);
                if (SF.isEmpty(tokenName)) {
                    return;
                }

                var serializer = new SF.Serializer().add({
                    webQueryName: this.options.webQueryName,
                    tokenName: tokenName,
                    index: this.newFilterRowIndex(),
                    prefix: this.options.prefix
                });
                if (!SF.isEmpty(requestExtraJsonData)) {
                    serializer.add(requestExtraJsonData);
                }

                var self = this;
                $.ajax({
                    url: url || SF.Urls.addFilter,
                    data: serializer.serialize(),
                    async: false,
                    success: function (filterHtml) {
                        var $filterList = self.element.closest(".sf-search-control").find(".sf-filters-list");
                        $filterList.find(".sf-explanation").hide();
                        $filterList.find("table").show();

                        tableFilters.append(filterHtml);
                        SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                    }
                });
            },

            newFilterRowIndex: function () {
                var lastRow = $(this.pf("tblFilters tbody tr:last"));
                var lastRowIndex = -1;
                if (lastRow.length == 1) {
                    lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
                }
                return parseInt(lastRowIndex) + 1;
            },

            newSubTokensComboAdded: function ($selectedCombo) {
                var $btnAddFilter = $(this.pf("btnAddFilter"));
                var $btnAddColumn = $(this.pf("btnAddColumn"));

                var self = this;
                var $selectedOption = $selectedCombo.children("option:selected");
                if ($selectedOption.val() == "") {
                    var $prevSelect = $selectedCombo.prev("select");
                    if ($prevSelect.length == 0) {
                        this.changeButtonState($btnAddFilter, lang.signum.selectToken);
                        this.changeButtonState($btnAddColumn, lang.signum.selectToken);
                    }
                    else {
                        var $prevSelectedOption = $prevSelect.find("option:selected");
                        this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () { self.addFilter(); });
                        this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () { self.addColumn(); });
                    }
                    return;
                }

                this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () { self.addFilter(); });
                this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () { self.addColumn(); });
            },

            changeButtonState: function ($button, disablingMessage, enableCallback) {
                var hiddenId = $button.attr("id") + "temp";
                if (typeof disablingMessage != "undefined") {
                    $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
                    $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
                }
                else {
                    var self = this;
                    $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
                    $button.unbind('click').bind('click', enableCallback);
                }
            },

            quickFilter: function (value, tokenName) {
                var tableFilters = $(this.pf("tblFilters tbody"));
                if (tableFilters.length === 0) {
                    return;
                }

                var params = {
                    "value": value,
                    "webQueryName": this.options.webQueryName,
                    "tokenName": tokenName,
                    "prefix": this.options.prefix,
                    "index": this.newFilterRowIndex()
                };

                var self = this;
                $.ajax({
                    url: SF.Urls.quickFilter,
                    data: params,
                    async: false,
                    success: function (filterHtml) {
                        var $filterList = self.element.find(".sf-filters-list");
                        $filterList.find(".sf-explanation").hide();
                        $filterList.find("table").show();

                        tableFilters.append(filterHtml);
                        SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                    }
                });
            },

            quickFilterCell: function ($elem) {
                var value;
                var data = $elem.children(".sf-data");
                if (data.length == 0) {
                    var cb = $elem.find("input:checkbox");
                    if (cb.length == 0) {
                        value = $elem.html().trim();
                    }
                    else {
                        value = cb.filter(":checked").length > 0;
                    }
                }
                else {
                    value = data.val();
                }

                var cellIndex = $elem[0].cellIndex;
                var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).children("input:hidden").val();

                this.quickFilter(value, tokenName);
            },

            quickFilterHeader: function ($elem) {
                this.quickFilter("", $elem.find("input:hidden").val());
            },

            create: function (viewOptions) {
                var self = this;
                var type = this.getRuntimeType(function (type) {
                    self.typedCreate($.extend({
                        type: type
                    }, viewOptions || {}));
                });
            },

            getRuntimeType: function (_onTypeFound) {
                var typeStr = $(this.pf(SF.Keys.entityTypeNames)).val();
                var types = typeStr.split(",");
                if (types.length == 1) {
                    return _onTypeFound(types[0]);
                }
                SF.openTypeChooser(this.options.prefix, _onTypeFound, { types: typeStr });
            },

            typedCreate: function (viewOptions) {
                viewOptions.prefix = viewOptions.prefix || this.options.prefix;
                if (SF.isEmpty(viewOptions.prefix)) {
                    var fullViewOptions = this.viewOptionsForSearchCreate(viewOptions);
                    new SF.ViewNavigator(fullViewOptions).navigate();
                }
                else {
                    var fullViewOptions = this.viewOptionsForSearchPopupCreate(viewOptions);
                    new SF.ViewNavigator(fullViewOptions).createSave();
                }
            },

            viewOptionsForSearchCreate: function (viewOptions) {
                return $.extend({
                    controllerUrl: SF.Urls.create
                }, viewOptions);
            },

            viewOptionsForSearchPopupCreate: function (viewOptions) {
                return $.extend({
                    controllerUrl: SF.Urls.popupCreate,
                    requestExtraJsonData: this.requestDataForSearchPopupCreate()
                }, viewOptions);
            },

            requestDataForSearchPopupCreate: function () {
                return {
                    filters: this.serializeFilters(),
                    webQueryName: this.options.webQueryName
                };
            },

            toggleSelectAll: function () {
                var select = $(this.pf("cbSelectAll:checked"));
                $("input:checkbox[name^=" + SF.compose(this.options.prefix, "rowSelection") + "]")
                    .attr('checked', (select.length > 0) ? true : false);
            },

            searchOnLoadFinished: false,

            searchOnLoad: function () {
                var btnSearchId = SF.compose(this.options.prefix, "qbSearch");
                var $button = $("#" + btnSearchId);
                var self = this;
                var makeSearch = function () {
                    if (!self.searchOnLoadFinished) {
                        $button.click();
                        self.searchOnLoadFinished = true;
                    }
                };

                var $tabContainer = $button.closest(".sf-tabs");
                if ($tabContainer.length == 0) {
                    makeSearch();
                }
                else {
                    var self = this;
                    $tabContainer.bind("tabsshow", function (evt, ui) {
                        if ($(ui.panel).find(self.element).length > 0) {
                            makeSearch();
                        }
                    });
                }
            }
        });
    })(jQuery);

    SF.FindNavigator = (function () {

        var getFor = function (prefix) {
            return $("#" + SF.compose(prefix, "sfSearchControl")).data("findNavigator");
        };

        var openFinder = function (findOptions) {
            var self = this;
            $.ajax({
                url: findOptions.openFinderUrl || (SF.isEmpty(findOptions.prefix) ? SF.Urls.find : SF.Urls.partialFind),
                data: this.requestDataForOpenFinder(findOptions),
                async: false,
                success: function (popupHtml) {
                    var divId = SF.compose(findOptions.prefix, "Temp");
                    $("body").append(SF.hiddenDiv(divId, popupHtml));
                    SF.triggerNewContent($("#" + divId));
                    $.extend(self.getFor(findOptions.prefix).options, findOptions); //Copy all properties (i.e. onOk was not transmitted)
                    $("#" + divId).popup({
                        onOk: function () { self.getFor(findOptions.prefix).onSearchOk(); },
                        onCancel: function () { self.getFor(findOptions.prefix).onSearchCancel(); }
                    });
                }
            });
        };

        var requestDataForOpenFinder = function (findOptions) {
            var requestData = {
                webQueryName: findOptions.webQueryName,
                elems: findOptions.elems,
                allowMultiple: findOptions.allowMultiple,
                prefix: findOptions.prefix
            };

            if (findOptions.view == false) {
                requestData["view"] = findOptions.view;
            }
            if (findOptions.searchOnLoad == true) {
                requestData["searchOnLoad"] = findOptions.searchOnLoad;
            }
            if (findOptions.filterMode != null) {
                requestData["filterMode"] = findOptions.filterMode;
            }
            if (!findOptions.create) {
                requestData["create"] = findOptions.create;
            }
            if (!findOptions.allowChangeColumns) {
                requestData["allowChangeColumns"] = findOptions.allowChangeColumns;
            }
            if (findOptions.filters != null) {
                requestData["filters"] = findOptions.filters;
            }
            if (findOptions.orders != null) {
                requestData["orders"] = this.serializeOrders(findOptions.orders);
            }
            if (findOptions.columns != null) {
                requestData["columns"] = findOptions.columns;
            }
            if (findOptions.columnMode != null) {
                requestData["columnMode"] = findOptions.columnMode;
            }

            return requestData;
        };

        var serializeOrders = function (orderArray) {
            var currOrders = orderArray.join(";");
            if (!SF.isEmpty(currOrders)) {
                currOrders += ";";
            }
            return currOrders; //.replace(/"/g, "");
        };

        var newSubTokensCombo = function (webQueryName, prefix, index, controllerUrl, requestExtraJsonData) {
            var $selectedCombo = $("#" + SF.compose(prefix, "ddlTokens_" + index));
            if ($selectedCombo.length == 0) {
                return;
            }

            this.clearChildSubtokenCombos($selectedCombo, prefix, index);

            var $container = $selectedCombo.closest(".sf-search-control");
            if ($container != null) {
                $container.trigger("sf-new-subtokens-combo", $selectedCombo.attr("id"));
            }

            var $selectedOption = $selectedCombo.children("option:selected");
            if ($selectedOption.val() == "") {
                return;
            }

            var serializer = new SF.Serializer().add({
                webQueryName: webQueryName,
                tokenName: this.constructTokenName(prefix),
                index: index,
                prefix: prefix
            });
            if (!SF.isEmpty(requestExtraJsonData)) {
                serializer.add(requestExtraJsonData);
            }

            var self = this;
            $.ajax({
                url: controllerUrl || SF.Urls.subTokensCombo,
                data: serializer.serialize(),
                dataType: "text",
                success: function (newCombo) {
                    if (!SF.isEmpty(newCombo)) {
                        $("#" + SF.compose(prefix, "ddlTokens_" + index)).after(newCombo);
                    }
                }
            });
        };

        var clearChildSubtokenCombos = function ($selectedCombo, prefix, index) {
            $selectedCombo.siblings("select,span")
                .filter(function () {
                    var elementId = $(this).attr("id");
                    if (typeof elementId == "undefined") {
                        return false;
                    }
                    if ((elementId.indexOf(SF.compose(prefix, "ddlTokens_")) != 0)
                        && (elementId.indexOf(SF.compose(prefix, "lblddlTokens_")) != 0)) {
                        return false;
                    }
                    var currentIndex = elementId.substring(elementId.lastIndexOf("_") + 1, elementId.length);
                    return parseInt(currentIndex) > index;
                })
                .remove();
        };

        var constructTokenName = function (prefix) {
            var tokenName = "";
            var stop = false;
            for (var i = 0; !stop; i++) {
                var currSubtoken = $("#" + SF.compose(prefix, "ddlTokens_" + i));
                if (currSubtoken.length > 0)
                    tokenName = SF.compose(tokenName, currSubtoken.val(), ".");
                else
                    stop = true;
            }
            return tokenName;
        };

        

        var deleteFilter = function (elem) {
            var $tr = $(elem).closest("tr");
            if ($tr.find("select[disabled]").length > 0) {
                return;
            }

            if ($tr.siblings().length == 0) {
                var $filterList = $tr.closest(".sf-filters-list");
                $filterList.find(".sf-explanation").show();
                $filterList.find("table").hide();
            }

            $tr.remove();
        };

        return {
            getFor: getFor,
            openFinder: openFinder,
            requestDataForOpenFinder: requestDataForOpenFinder,
            serializeOrders: serializeOrders,
            newSubTokensCombo: newSubTokensCombo,
            clearChildSubtokenCombos: clearChildSubtokenCombos,
            constructTokenName: constructTokenName,
            deleteFilter: deleteFilter
        }
    })();
});