$(function () {
    // 1. LẤY TOKEN BẢO MẬT TỪ THẺ META
    const csrfToken = $('meta[name="csrf-token"]').attr('content');

    // 2. HÀM TẢI LẠI DỮ LIỆU LƯỚI SIÊU MƯỢT (KHÔNG LOAD LẠI TRANG)
    function reloadGridData() {
        $.get('/ReportManager/GetReportsList', function (newData) {
            // Đẩy dữ liệu mới vào lưới và yêu cầu nó tự vẽ lại
            var gridInstance = $("#gridContainer").dxDataGrid("instance");
            gridInstance.option("dataSource", newData);
            gridInstance.refresh();
        });
    }

    // 3. KHỞI TẠO DATAGRID (Lúc đầu gọi hàm API để lấy data luôn)
    $("#gridContainer").dxDataGrid({
        dataSource: '/ReportManager/GetReportsList', // Gọi thẳng API lúc khởi tạo
        showBorders: true,
        rowAlternationEnabled: true,
        searchPanel: { visible: true, width: 300, placeholder: "Tìm kiếm báo cáo..." },
        paging: { pageSize: 15 },
        columns: [
            { dataField: "name", caption: "Tên Báo Cáo", cssClass: "fw-bold" },
            { dataField: "tenantId", caption: "Khách Hàng", width: 200 },
            { dataField: "sizeKB", caption: "Dung Lượng (KB)", width: 150, alignment: "right" },
            { dataField: "updatedAt", caption: "Cập Nhật Lần Cuối", dataType: "datetime", format: "dd/MM/yyyy HH:mm", width: 200 },
            {
                type: "buttons",
                width: 120,
                buttons: [
                    {
                        hint: "Export (.repx)",
                        icon: "download",
                        cssClass: "text-info",
                        onClick: function (e) {
                            window.location.href = '/ReportManager/Export?id=' + e.row.data.id;
                        }
                    },
                    {
                        hint: "Xóa báo cáo",
                        icon: "trash",
                        cssClass: "text-danger",
                        onClick: function (e) {
                            var rowData = e.row.data;
                            var result = DevExpress.ui.dialog.confirm(
                                `<i>Bạn có chắc muốn xóa mẫu in <b>${rowData.name}</b> không?</i>`,
                                "Xác nhận xóa"
                            );

                            result.done(function (dialogResult) {
                                if (dialogResult) {
                                    $.ajax({
                                        url: '/ReportManager/Delete',
                                        type: 'POST',
                                        data: { id: rowData.id, __RequestVerificationToken: csrfToken },
                                        success: function (res) {
                                            DevExpress.ui.notify(res, "success", 2000);
                                            reloadGridData(); // LOAD LẠI LƯỚI MƯỢT MÀ
                                        },
                                        error: function (xhr) {
                                            DevExpress.ui.notify("Lỗi: " + xhr.responseText, "error", 4000);
                                        }
                                    });
                                }
                            });
                        }
                    }
                ]
            }
        ],
        onToolbarPreparing: function (e) {
            e.toolbarOptions.items.unshift({
                location: "after",
                widget: "dxButton",
                options: {
                    icon: "upload", text: "Import File", type: "default",
                    onClick: function () { $('#importModal').modal('show'); }
                }
            });
        }
    });

    // 4. XỬ LÝ SỰ KIỆN CHỌN FILE (HIỆN DUNG LƯỢNG)
    $('#fileUploadInput').on('change', function () {
        var file = this.files[0];
        if (file) {
            var sizeKB = (file.size / 1024).toFixed(2);
            $('#fileSizeInfo').text('✔️ Đã nạp file: ' + file.name + ' (' + sizeKB + ' KB)').show();
        } else {
            $('#fileSizeInfo').hide();
        }
    });

    // 5. XỬ LÝ UPLOAD BẰNG AJAX
    $('#btnUploadAjax').click(function () {
        var formElement = $('#frmImportFile')[0];
        if (!formElement.checkValidity()) {
            formElement.reportValidity();
            return;
        }

        var formData = new FormData(formElement);
        formData.append('__RequestVerificationToken', csrfToken); // Đính kèm token bảo mật

        var btn = $(this);
        btn.prop('disabled', true).text('Đang xử lý...');

        $.ajax({
            url: '/ReportManager/Import',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            timeout: 60000,
            success: function (res) {
                $('#importModal').modal('hide');
                formElement.reset(); // Xóa trắng form cũ
                $('#fileSizeInfo').hide(); // Ẩn chữ hiển thị size file
                btn.prop('disabled', false).text('Import'); // Mở khóa nút

                DevExpress.ui.notify("Import dữ liệu thành công!", "success", 2000);

                reloadGridData(); // LOAD LẠI LƯỚI MƯỢT MÀ
            },
            error: function (xhr) {
                var errorText = xhr.responseText ? xhr.responseText : "Lỗi không xác định";
                alert("Lỗi Upload: " + errorText);
                btn.prop('disabled', false).text('Import');
            }
        });
    });
});