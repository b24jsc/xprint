// Đảm bảo hàm được khai báo ngay khi file JS tải xong
window.closeImportPopup = function () {
    // Nếu anh dùng dxPopup cho Import
    var popup = $("#importPopup").dxPopup("instance");
    if (popup) {
        popup.hide();
    } else {
        // Nếu anh vẫn dùng Modal của Bootstrap cũ
        $('#importModal').modal('hide');
    }
};

window.submitImportForm = function (e) {
    var formElement = $('#frmImportFile')[0];

    // Kiểm tra tính hợp lệ của Form (file, text...)
    if (!formElement.checkValidity()) {
        formElement.reportValidity();
        return;
    }

    var formData = new FormData(formElement);
    // Nhúng Token bảo mật
    var csrfToken = $('meta[name="csrf-token"]').attr('content');
    formData.append('__RequestVerificationToken', csrfToken);

    // Lấy instance của chính cái nút vừa bấm để đổi trạng thái
    var btn = e.component;
    btn.option("text", "Đang tải...");
    btn.option("disabled", true);

    $.ajax({
        url: '/Reports/Import',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (res) {
            window.closeImportPopup();
            formElement.reset();
            DevExpress.ui.notify(res, "success", 2000);

            // Gọi hàm refreshGrid ở file reports-index.js
            if (typeof window.reloadGridData === 'function') {
                window.reloadGridData();
            } else if (typeof window.refreshGrid === 'function') {
                window.refreshGrid();
            }
        },
        error: function (xhr) {
            DevExpress.ui.notify(xhr.responseText, "error", 3000);
        },
        complete: function () {
            // Khôi phục lại nút
            btn.option("text", "Import");
            btn.option("disabled", false);
        }
    });
};