# Báo cáo Trạng thái Giao tiếp Backend (API vs Mock)

Dưới đây là danh sách các chức năng trong dự án Frontend và trạng thái giao tiếp thực tế với Backend.

## 🟢 Chức năng đã giao tiếp với Backend (Live)

Các chức năng này đã được cấu hình để gửi/nhận dữ liệu từ API tại `http://localhost:7010/api`.

1. **Xác thực người dùng (Authentication)**
   - **Đăng nhập (`/auth/login`)**: Gửi thông tin credentials và nhận JWT token.
   - **Đăng xuất (`/auth/logout`)**: Gọi API để hủy phiên làm việc.
   - **Quản lý Token**: Đã có interceptor tự động đính kèm Bearer Token vào header của các request.
   - *Tệp tin liên quan:* `src/services/authService.js`, `src/stores/authStore.js`

2. **Quản lý Nhân viên (Employee Management)**
   - **Lấy danh sách (`GET /Employees`)**: Tải dữ liệu nhân viên từ cơ sở dữ liệu.
   - **Thêm mới (`POST /Employees`)**: Lưu thông tin nhân viên mới vào backend.
   - **Cập nhật (`PUT /Employees/{id}`)**: Chỉnh sửa thông tin nhân viên hiện có.
   - **Nghỉ việc/Xóa (`PUT /Employees/{id}/terminate`)**: Cập nhật trạng thái nhân viên trên hệ thống.
   - *Lưu ý:* Vẫn còn cơ chế fallback (dữ liệu mẫu) nếu API backend không hoạt động.
   - *Tệp tin liên quan:* `src/stores/employeeStore.js`, `src/components/Employees.vue`

---

## 🟡 Chức năng đang sử dụng Mock API / Dữ liệu mẫu (Mock)

Các chức năng này hiện đang chỉ hoạt động hoàn toàn ở phía Frontend bằng cách sử dụng dữ liệu giả lập, LocalStorage hoặc biến cục bộ.

1. **Bảng điều khiển tổng quan (Dashboard)**
   - Biểu đồ (Unovis) và các con số thống kê hiện đang sử dụng dữ liệu JavaScript ngẫu nhiên (`Math.random()`).
   - Chưa có API endpoint để lấy dữ liệu thực tế cho biểu đồ.
   - *Tệp tin liên quan:* `src/components/Dashboard.vue`

2. **Chấm công hàng ngày (Daily Attendance)**
   - Trạng thái "Có mặt/Vắng" được lưu trong biến cục bộ của component và sẽ mất khi tải lại trang.
   - Chưa có API để lưu bảng chấm công vào cơ sở dữ liệu.
   - *Tệp tin liên quan:* `src/components/AttendanceDashboard.vue`

3. **Chấm công theo năm (Yearly Attendance)**
   - Dữ liệu tổng hợp công và giờ OT được tạo ngẫu nhiên cho từng tháng.
   - *Tệp tin liên quan:* `src/components/YearlyAttendance.vue`

4. **Tuyển dụng (Recruitment)**
   - Danh sách tin tuyển dụng và các thao tác (Thêm, Sửa, Xóa, Nhân bản) được quản lý trong state cục bộ (`ref`).
   - *Tệp tin liên quan:* `src/components/Recruitment.vue`

5. **Đào tạo (Training)**
   - Dữ liệu khóa học được lưu trữ và truy xuất từ **LocalStorage** của trình duyệt. Dữ liệu này chỉ tồn tại trên máy của người dùng.
   - *Tệp tin liên quan:* `src/components/Training.vue`

6. **Cài đặt cá nhân (Settings)**
   - Thông tin hồ sơ được hardcoded trong component. Chức năng "Save Changes" hiện chỉ ghi log ra console mà chưa gọi API cập nhật.
   - *Tệp tin liên quan:* `src/components/Setting.vue`

7. **Nghỉ phép (Leave)**
   - Hiện tại chỉ là một trang trống với dòng chữ placeholder.
   - *Tệp tin liên quan:* `src/components/Leave.vue`
