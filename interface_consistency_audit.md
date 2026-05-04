# Báo cáo Kiểm tra Tính Đồng nhất Giao diện (Interface Consistency Audit)

Sau khi quét toàn bộ mã nguồn Frontend (`src/components` và `src/assets`), tôi xác nhận rằng giao diện hiện tại đang gặp tình trạng **thiếu đồng nhất nghiêm trọng** và cấu trúc code UI đang khá **lộn xộn**. Dự án hiện giống như một tập hợp các trang web lẻ tẻ hơn là một nền tảng thống nhất.

Dưới đây là các phân tích chi tiết:

## 1. Sự lộn xộn trong quản lý Style (CSS)
*   **Cách tiếp cận không thống nhất**: 
    *   `Login.vue`: Sử dụng `<style>` nội bộ cực lớn (hơn 300 dòng).
    *   `Employees.vue`, `Dashboard.vue`: Import file `.css` riêng biệt.
    *   `base.css`: Các biến hệ thống cốt lõi bị comment lại, dẫn đến việc các component phải tự định nghĩa màu sắc riêng.
*   **Lỗi đặt tên**: Xuất hiện các file sai chính tả như `Emploees.css`, và sự pha trộn giữa PascalCase (`AttendanceDashboard.css`) và lowercase (`base.css`).

## 2. Thiếu tính tái sử dụng (Component Reuse)
Dự án đang vi phạm nguyên tắc **Atomic Design** trong bộ quy tắc `frontend.md`:
*   **Tables (Bảng)**: Mỗi trang (`Employees`, `Recruitment`, `Dashboard`, `Attendance`) đều tự viết lại thẻ `<table>` và style riêng cho `thead`, `tbody`. Điều này khiến khoảng cách, cỡ chữ và màu sắc hàng/cột không đồng đều giữa các trang.
*   **Modals (Hộp thoại)**: Ít nhất 3 cách triển khai Modal khác nhau được tìm thấy. CSS cho modal bị lặp lại nhiều lần thay vì dùng một BaseModal chung.
*   **Forms (Ô nhập liệu)**: Các class `input-group`, `form-input` được định nghĩa lặp đi lặp lại với các thuộc tính CSS hơi khác nhau ở mỗi trang.

## 3. Sự khác biệt về cấu trúc Layout & Typography
*   **Cấu trúc Header**: Mỗi trang có một "kiểu" tiêu đề riêng:
    *   Dashboard: `.header > .title`
    *   Employees: `.header > .title-section > .page-title`
    *   Recruitment: `.main-content > .page-title-container > h1`
*   **Hệ thống màu sắc**: Đang sử dụng quá nhiều mã Hex cứng (`#1e293b`, `#f8fafc`, `#2196F3`) thay vì dùng hệ thống biến HSL như quy định trong `AESTHETICS.md`.

## 4. Vi phạm quy tắc Thẩm mỹ (AESTHETICS.md)
Mặc dù bộ quy tắc yêu cầu chuẩn **Magic UI & Glassmorphism**, thực tế:
*   Hầu hết các nền vẫn dùng màu đặc (Solid) thay vì `bg-white/10` kết hợp `backdrop-blur`.
*   Viền (borders) vẫn khá thô, chưa đạt độ mảnh 0.5px - 1px như yêu cầu.
*   Thiếu hoàn toàn các hiệu ứng chuyển động tinh tế (`Staggered Entrance`) khi chuyển trang hoặc mở modal.

## 5. Vấn đề về chất lượng Code UI
*   Các file như `Employees.vue` (hơn 1000 dòng) và `Recruitment.vue` (hơn 600 dòng) đang quá tải. Chúng chứa cả Template, Logic xử lý dữ liệu phức tạp, CSS khổng lồ và dữ liệu Mock. Điều này cực kỳ khó bảo trì và dễ gây ra lỗi hiển thị khi sửa đổi.

## 6. Trải nghiệm người dùng (UX) và Sự khó hiểu
Ngoài việc lộn xộn về thị giác, giao diện hiện tại còn gây **khó hiểu và ức chế** cho người dùng:
*   **Ngôn ngữ hỗn tạp (Vietlish)**: Hiện tại có sự trộn lẫn giữa tiếng Anh và tiếng Việt không kiểm soát (ví dụ: nút "Edit Profile", "Save Changes" bên cạnh "Chào mừng trở lại"). Người dùng không chuyên môn sẽ cảm thấy bối rối.
*   **Menu chồng chéo**: Có quá nhiều mục thực tế đang trỏ về cùng một trang Dashboard hoặc trang Chấm công (như "Báo cáo", "Đào tạo", "Xem bảng lương"). Điều này khiến người dùng cảm thấy như đang bị "lạc" trong một mê cung không có nội dung thực.
*   **Thiếu điều hướng trực quan**: Các nút hành động chính (Primary Actions) không có sự phân cấp rõ ràng. Người dùng khó nhận biết đâu là nút cần bấm tiếp theo để hoàn thành một quy trình (ví dụ: sau khi quét QR xong thì làm gì tiếp theo?).
*   **Phản hồi hệ thống (Feedback)**: Sự thiếu hụt các hiệu ứng Loading, Success/Error toasts một cách đồng bộ khiến người dùng không biết thao tác của mình đã thực sự thành công hay chưa.

---

## 🚀 Đề xuất cải tiến (Action Plan)
1.  **Xây dựng bộ UI Kit chung**: Trích xuất các thành phần `Button`, `Input`, `Modal`, `Table` thành các Component dùng chung.
2.  **Hệ thống hóa CSS Variables**: Kích hoạt lại `base.css`, định nghĩa bảng màu HSL và hệ thống spacing (4px/8px) để áp dụng toàn dự án.
3.  **Refactor Layout**: Thống nhất một cấu trúc Header và Padding cho tất cả các trang "Main Content".
4.  **Áp dụng Glassmorphism**: Thay thế dần các background đặc bằng hiệu ứng mờ kính để đúng với định hướng thẩm mỹ ban đầu.

**Kết luận:** Giao diện hiện tại **chưa đạt chuẩn chuyên nghiệp**. Cần một đợt Refactor tổng thể về UI/UX để đảm bảo tính thương hiệu và trải nghiệm người dùng đồng nhất.
