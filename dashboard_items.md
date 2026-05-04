# Danh sách các mục trong giao diện Dashboard

Dựa trên việc quét mã nguồn frontend (`Sidebar.vue` và `Dashboard.vue`), dưới đây là danh sách các mục và thành phần trong giao diện dashboard:

## 1. Menu điều hướng (Sidebar)
Menu bên trái bao gồm các mục chính sau:

- **Tổng quan**: Trang chính hiển thị các biểu đồ và thống kê.
- **Danh sách nhân viên**: Quản lý thông tin nhân sự.
- **Chấm công**: (Có menu con)
    - **Chấm theo ngày**: Theo dõi chấm công hàng ngày.
    - **Chấm theo năm**: Xem báo cáo chấm công theo năm.
- **Lương thưởng**: Quản lý lương và các khoản thưởng.
- **Nghỉ phép**: Quản lý đơn xin nghỉ phép.
- **Tuyển dụng**: Quản lý quy trình tuyển dụng.
- **Đào tạo**: Quản lý các khóa đào tạo nhân viên.
- **Báo cáo**: Xuất các báo cáo nhân sự, chấm công, lương.
- **Cài đặt**: Cấu hình hệ thống.
- **Đăng xuất**: Thoát khỏi hệ thống.

## 2. Các thành phần trên trang Dashboard chính
Khi ở mục "Tổng quan", giao diện hiển thị:

- **Header**:
    - Tiêu đề "Dashboard".
    - Ô tìm kiếm (Search box).
    - Nút **Quét chấm công**: Mở modal quét mã QR để chấm công.
- **Biểu đồ thống kê**: Một biểu đồ cột lớn (Grouped Bar Chart) hiển thị dữ liệu (mặc định là Desktop vs Mobile theo tháng).
- **Bảng thống kê nhanh**: Hiển thị danh sách các mục chính với các cột:
    - Mục (Tương ứng với các menu chính).
    - Số lượng.
    - Tỷ lệ (Thanh tiến trình).
    - Trạng thái (Hoạt động/Tạm dừng).

## 3. Các mục bạn hỏi cụ thể
Dựa trên yêu cầu của bạn, đây là hiện trạng của các mục cụ thể:

- **Báo cáo công việc**: Có mục **"Báo cáo"** trong menu, nhưng hiện tại nó đang trỏ về trang Dashboard chung (chưa có trang báo cáo công việc riêng biệt).
- **Yêu cầu**: **Chưa có** mục này trong giao diện hiện tại.
- **Góp ý**: **Chưa có** mục này trong giao diện hiện tại.
- **Điểm danh**: Đã có và nằm trong mục **"Chấm công"**. Giao diện `AttendanceDashboard.vue` xác định đây là nơi "Giám sát hoạt động chấm công và điểm danh nhân viên".
- **Xem bảng lương**: Nằm trong mục **"Lương thưởng"**. Tuy nhiên, hiện tại mục này cũng đang trỏ về trang Dashboard chung với dữ liệu mô phỏng, chưa có trang chi tiết bảng lương cá nhân.
- **Xem thông tin cá nhân**: Có thể xem và chỉnh sửa thông tin cá nhân (Họ tên, Email, Ảnh đại diện, Bio, v.v.) trong mục **"Cài đặt"** (Setting). 

## 4. Các tính năng bổ trợ
- **QR Scanner**: Công cụ quét mã QR tích hợp để chấm công nhanh.
- **Đồng hồ thời gian thực**: Hiển thị thời gian và ngày tháng hiện tại.

