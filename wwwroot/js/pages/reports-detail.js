$(function () {
    const csrfToken = $('meta[name="csrf-token"]').attr('content');

    // Hàm khởi tạo dxButton sau khi HTML được load vào Popup
    // Bắt sự kiện contentReady của Popup Detail (Từ index)
    if (window.detailPopup) {
        window.detailPopup.on("contentReady", function (e) {
            // Khởi tạo nút format
            $("#btnFormatJson").dxButton({
                text: "Định dạng JSON",
                icon: "indent",
                stylingMode: "text",
                type: "default",
                onClick: function () {
                    try {
                        const obj = JSON.parse($('#jsonEditor').val());
                        $('#jsonEditor').val(JSON.stringify(obj, null, 4));
                        DevExpress.ui.notify("Đã căn chỉnh JSON", "info", 1500);
                    } catch (err) {
                        DevExpress.ui.notify("JSON không hợp lệ!", "error", 2000);
                    }
                }
            });

            // Khởi tạo nút Save
            $("#btnSaveJsonDetail").dxButton({
                text: "LƯU CẤU HÌNH",
                icon: "save",
                type: "success", // Nút xanh lá chuẩn DX
                onClick: function (e) {
                    const btn = e.component;
                    const formElement = $('#frmUpdateJson')[0];

                    const formData = new FormData(formElement);
                    formData.append('__RequestVerificationToken', csrfToken);

                    btn.option("text", "Đang lưu...");
                    btn.option("disabled", true);

                    $.ajax({
                        url: '/Reports/UpdateJsonSchema', type: 'POST', data: formData, processData: false, contentType: false,
                        success: function (res) {
                            DevExpress.ui.notify(res, "success", 2000);
                            window.detailPopup.hide();
                            if (window.reloadGridData) window.reloadGridData();
                        },
                        error: function (xhr) {
                            DevExpress.ui.notify(xhr.responseText || "Lỗi lưu dữ liệu", "error", 2000);
                        },
                        complete: function () {
                            btn.option("text", "LƯU CẤU HÌNH");
                            btn.option("disabled", false);
                        }
                    });
                }
            });
        });
    }
});