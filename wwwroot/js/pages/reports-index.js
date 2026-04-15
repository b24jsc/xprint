$(function () {
    const csrfToken = $('meta[name="csrf-token"]').attr('content');

    // 1. KHỞI TẠO POPUP IMPORT (DEVEXTREME)
    window.importPopup = $("#importPopup").dxPopup({
        width: 500,
        height: 'auto',
        showTitle: true,
        title: "Import File (.repx)",
        visible: false,
        dragEnabled: true,
        closeOnOutsideClick: true,
        showCloseButton: true,
    }).dxPopup("instance");

    // 2. KHỞI TẠO POPUP CHI TIẾT
    window.detailPopup = $("#detailPopup").dxPopup({
        width: 700,
        height: '85%',
        showTitle: true,
        title: "Chi tiết & Cấu hình JSON",
        visible: false,
        dragEnabled: true,
        closeOnOutsideClick: true,
        showCloseButton: true,
    }).dxPopup("instance");

    // 3. KHỞI TẠO DATAGRID KÈM TOOLBAR
    const dataGrid = $("#gridContainer").dxDataGrid({
        dataSource: '/Reports/GetReportsList',
        showBorders: true,
        rowAlternationEnabled: true,
        hoverStateEnabled: true,
        searchPanel: { visible: true, width: 300, placeholder: "Tìm kiếm mẫu in..." },
        paging: { pageSize: 12 },
        onToolbarPreparing: function (e) {
            let toolbarItems = e.toolbarOptions.items;

            toolbarItems.unshift({
                location: 'before',
                template: () => $('<h2>').addClass('fw-bold mb-0').css('font-size', '20px').text('Quản lý mẫu in')
            });

            toolbarItems.push({
                location: 'after', widget: 'dxButton',
                options: { icon: 'refresh', text: 'Làm mới', onClick: () => dataGrid.refresh() }
            });

            toolbarItems.push({
                location: 'after', widget: 'dxButton',
                options: { icon: 'upload', text: 'Import mới', type: 'default', onClick: () => window.importPopup.show() }
            });
        },
        columns: [
            { dataField: "name", caption: "Tên Mẫu In", cssClass: "fw-bold" },
            { dataField: "tenantId", caption: "Khách Hàng (Tenant)", width: 200 },
            { dataField: "updatedAt", caption: "Cập Nhật Cuối", dataType: "datetime", format: "dd/MM/yyyy HH:mm", width: 160 },
            {
                type: "buttons", width: 120,
                buttons: [
                    { hint: "Cấu hình JSON", icon: "textdocument", onClick: (e) => openDetailPanel(e.row.data.id) },
                    { hint: "Tải file", icon: "download", onClick: (e) => window.location.href = '/Reports/Export?id=' + e.row.data.id },
                    { hint: "Xóa", icon: "trash", cssClass: "dx-theme-accent-as-text-color", onClick: (e) => deleteReportItem(e.row.data) }
                ]
            }
        ]
    }).dxDataGrid("instance");

    // HÀM TOÀN CỤC CHO CÁC FILE KHÁC GỌI
    window.reloadGridData = function () {
        dataGrid.refresh();
    };

    function openDetailPanel(id) {
        window.detailPopup.show();
        var popupContentArea = window.detailPopup.content();
        popupContentArea.html('<div class="d-flex justify-content-center mt-5"><div class="spinner-border text-primary me-2"></div> Đang tải dữ liệu...</div>');

        $.get('/Reports/GetDetailPartial?id=' + id, function (html) {
            popupContentArea.html(html);
        }).fail(function () {
            popupContentArea.html('<div class="alert alert-danger m-3">Lỗi tải dữ liệu!</div>');
        });
    }

    function deleteReportItem(data) {
        DevExpress.ui.dialog.confirm(`Xóa báo cáo <b>${data.name}</b>?`, "Xác nhận xóa").done(function (dialogResult) {
            if (dialogResult) {
                $.post('/Reports/Delete', { id: data.id, __RequestVerificationToken: csrfToken }, function (res) {
                    DevExpress.ui.notify(res, "success", 2000);
                    window.reloadGridData();
                }).fail(function (xhr) {
                    DevExpress.ui.notify("Lỗi: " + xhr.responseText, "error", 3000);
                });
            }
        });
    }
});