$(function () {
    // 1. TÍNH NĂNG BẮT KÍCH THƯỚC FILE KHI CHỌN
    $(document).on('change', '#fileUploadInput', function () {
        var file = this.files[0];
        if (file) {
            // Chuyển đổi Bytes sang KB
            var sizeKB = (file.size / 1024).toFixed(2);
            $('#fileSizeInfo')
                .html('<i class="dx-icon-check"></i> Đã chọn file: ' + file.name + ' (' + sizeKB + ' KB)')
                .fadeIn();
        } else {
            $('#fileSizeInfo').hide();
        }
    });

    // 2. TÍNH NĂNG UPLOAD AJAX
    $(document).on('click', '#btnUploadAjax', function () {
        var formElement = $('#frmImportFile')[0];

        // Ép trình duyệt kiểm tra các ô required
        if (!formElement.checkValidity()) {
            formElement.reportValidity();
            return;
        }

        // Gom dữ liệu Form và Token bảo mật
        var formData = new FormData(formElement);
        formData.append('__RequestVerificationToken', $('meta[name="csrf-token"]').attr('content'));

        var btn = $(this);

        // Hiển thị loading trên nút bấm
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang tải...');

        $.ajax({
            url: '/Reports/Import',
            type: 'POST',
            data: formData,
            processData: false, // Bắt buộc false để file không bị biến thành chuỗi
            contentType: false, // Bắt buộc false để trình duyệt tự set boundary chuẩn
            success: function (res) {
                // 1. Đóng Modal
                $('#importModal').modal('hide');

                // 2. Xóa trắng Form và chữ dung lượng để lần sau upload không bị dính file cũ
                formElement.reset();
                $('#fileSizeInfo').hide();

                // 3. Khôi phục nút bấm
                btn.prop('disabled', false).text('Import');

                // 4. Báo thành công
                DevExpress.ui.notify(res, "success", 2000);

                // 5. Load lại lưới dữ liệu bên trang Index (Gọi chéo hàm)
                if (typeof reloadGridData === 'function') {
                    reloadGridData();
                }
            },
            error: function (xhr) {
                alert("Lỗi Upload: " + xhr.responseText);
                btn.prop('disabled', false).text('Import');
            }
        });
    });
});