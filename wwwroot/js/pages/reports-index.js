$(function () {
    // 1. KHỞI TẠO POPUP CỦA DEVEXTREME (Thay thế cho Offcanvas bị lỗi)
    var detailPopup = $("#detailPopup").dxPopup({
        width: 700,
        height: '85%',
        showTitle: true,
        title: "Chi tiết & Cấu hình JSON",
        visible: false, // Ban đầu ẩn đi
        dragEnabled: true,
        closeOnOutsideClick: true,
        showCloseButton: true,
    }).dxPopup("instance");


    // 2. KHỞI TẠO LƯỚI DEVEXTREME
    $("#gridContainer").dxDataGrid({
        dataSource: '/Reports/GetReportsList',
        showBorders: false,
        rowAlternationEnabled: true,
        hoverStateEnabled: true,
        searchPanel: { visible: true, width: 300, placeholder: "Tìm kiếm..." },
        paging: { pageSize: 12 },
        columns: [
            { dataField: "name", caption: "Tên Mẫu In", cssClass: "fw-bold" },
            { dataField: "tenantId", caption: "Khách Hàng (Tenant)", width: 200 },
            { dataField: "updatedAt", caption: "Cập Nhật Cuối", dataType: "datetime", format: "dd/MM/yyyy HH:mm", width: 160 },
            {
                type: "buttons", width: 140,
                buttons: [
                    {
                        hint: "Chi tiết & Cấu hình JSON", icon: "textdocument", cssClass: "text-primary",
                        onClick: function (e) {
                            window.openDetailPanel(e.row.data.id);
                        }
                    },
                    {
                        hint: "Tải file (.repx)", icon: "download", cssClass: "text-info",
                        onClick: function (e) {
                            window.location.href = '/Reports/Export?id=' + e.row.data.id;
                        }
                    },
                    {
                        hint: "Xóa báo cáo", icon: "trash", cssClass: "text-danger",
                        onClick: function (e) {
                            window.deleteReportItem(e.row.data);
                        }
                    }
                ]
            }
        ]
    });

    // 3. HÀM MỞ POPUP CHI TIẾT
    window.openDetailPanel = function (id) {
        // 3.1. Hiện Popup lên trước
        detailPopup.show();

        // 3.2. Lấy đối tượng chứa nội dung CHÍNH CHỦ của DevExtreme
        // Biến này sẽ luôn luôn trỏ đúng vào ruột của Popup dù nó bay đi đâu
        var popupContentArea = detailPopup.content();

        // 3.3. Hiện chữ Đang tải
        popupContentArea.html('<div class="text-center mt-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div> Đang tải dữ liệu...</div>');

        // 3.4. Bơm HTML từ API vào thẳng ruột Popup
        $.get('/Reports/GetDetailPartial?id=' + id, function (html) {
            popupContentArea.html(html);
        }).fail(function () {
            popupContentArea.html('<div class="alert alert-danger m-3">Lỗi khi tải dữ liệu!</div>');
        });
    };

    // 4. HÀM XÓA
    window.deleteReportItem = function (data) {
        var result = DevExpress.ui.dialog.confirm(`Bạn có chắc muốn xóa <b>${data.name}</b> không?`, "Xác nhận xóa");
        result.done(function (dialogResult) {
            if (dialogResult) {
                $.ajax({
                    url: '/Reports/Delete', type: 'POST',
                    data: { id: data.id, __RequestVerificationToken: $('meta[name="csrf-token"]').attr('content') },
                    success: function (res) {
                        DevExpress.ui.notify(res, "success", 2000);
                        window.reloadGridData();
                    },
                    error: function (xhr) { alert("Lỗi khi xóa: " + xhr.responseText); }
                });
            }
        });
    };

    // 5. HÀM RELOAD LƯỚI
    window.reloadGridData = function () {
        $("#gridContainer").dxDataGrid("instance").refresh();
    };
});