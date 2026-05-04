<template>
  <div class="setting-page">
    <Sidebar activeItem="Cài đặt" />

    <main class="setting-main">
      <header class="setting-header glass">
        <div class="header-left">
          <h1 class="setting-title">Thông tin cá nhân</h1>
          <p class="page-subtitle">Quản lý tài khoản và thiết lập hệ thống</p>
        </div>
        <div class="header-right">
          <template v-if="!isEditing">
            <BaseButton 
              variant="secondary" 
              @click="showPersonalQR"
              title="Xem mã QR cá nhân"
            >
              <template #icon-left>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                  <line x1="9" y1="9" x2="15" y2="15" />
                  <line x1="15" y1="9" x2="9" y2="15" />
                </svg>
              </template>
              QR Cá nhân
            </BaseButton>
            <BaseButton 
              v-if="authStore.user?.role === 'employee'"
              variant="secondary" 
              @click="showQRScanner = true"
            >
              <template #icon-left>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                  <path d="M7 7h.01M17 7h.01M7 17h.01M17 17h.01M12 7v10M7 12h10" />
                </svg>
              </template>
              Quét mã chấm công
            </BaseButton>
            <BaseButton variant="primary" @click="enableEditing">
              <template #icon-left>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
                </svg>
              </template>
              Chỉnh sửa thông tin
            </BaseButton>
          </template>
          <template v-else>
            <BaseButton variant="secondary" @click="cancelEditing">Hủy bỏ</BaseButton>
            <BaseButton variant="primary" @click="saveChanges">Lưu thay đổi</BaseButton>
          </template>
        </div>
      </header>

      <!-- Success Message -->
      <div v-if="showSuccessMessage" class="success-message">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 6L9 17l-5-5" />
        </svg>
        <span>{{ successMessage }}</span>
      </div>

      <div class="setting-content">
        <div class="setting-grid">
          <!-- Cột trái: Profile Card + Security Card -->
          <div class="profile-column">
            <BaseCard class="profile-card" :hover="true">
              <div class="avatar-section">
                <div class="avatar-wrapper" :class="{ 'editable': isEditing }" @click="triggerFileInput">
                  <div class="avatar-container">
                    <img :src="user.avatar || defaultAvatar" alt="Avatar" class="user-avatar" />
                    <div v-if="isEditing" class="avatar-overlay">
                      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2">
                        <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" />
                        <circle cx="12" cy="13" r="3" />
                      </svg>
                      <span>Đổi ảnh</span>
                    </div>
                  </div>
                </div>
                <input 
                  type="file" 
                  ref="fileInput" 
                  hidden 
                  @change="handleAvatarChange" 
                  accept="image/*" 
                />
                <h2 class="user-name">{{ user.fullName || 'Chưa cập nhật' }}</h2>
                <p class="user-role">{{ user.position || 'Nhân viên' }}</p>
                <div class="user-badge">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                    <line x1="9" y1="9" x2="15" y2="15" />
                    <line x1="15" y1="9" x2="9" y2="15" />
                  </svg>
                  {{ user.employeeId || 'NV-001' }}
                </div>
              </div>
            </BaseCard>

            <!-- Security Card -->
            <BaseCard class="security-card">
              <h3 class="section-title">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
                Bảo mật
              </h3>
              <div class="security-content">
                <div class="security-item">
                  <div class="security-info">
                    <span class="security-label">Mật khẩu</span>
                    <span class="security-dots">
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                      <span class="dot"></span>
                    </span>
                  </div>
                  <BaseButton 
                    variant="ghost" 
                    size="sm"
                    class="security-btn"
                    @click="openPasswordModal"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                      <circle cx="12" cy="7" r="4" />
                    </svg>
                    Đổi mật khẩu
                  </BaseButton>
                </div>
                <div class="security-hint">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="16" x2="12" y2="12" />
                    <line x1="12" y1="8" x2="12.01" y2="8" />
                  </svg>
                  <span>Nên đặt mật khẩu mạnh để bảo vệ tài khoản</span>
                </div>
              </div>
            </BaseCard>
          </div>

          <!-- Cột phải: Thông tin cá nhân, công việc và giới thiệu -->
          <div class="form-column">
            <BaseCard class="info-card">
              <h3 class="section-title">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
                Thông tin cá nhân
              </h3>
              <div class="info-rows">
                <div class="info-row">
                  <div class="info-cell">
                    <span class="info-cell-label">Họ và tên</span>
                    <span class="info-cell-value read-only">{{ user.fullName || 'Chưa cập nhật' }}</span>
                  </div>
                  <div class="info-cell">
                    <span class="info-cell-label">Email</span>
                    <span class="info-cell-value read-only">{{ user.email || 'Chưa cập nhật' }}</span>
                  </div>
                </div>
                <div class="info-row">
                  <div class="info-cell">
                    <span class="info-cell-label">Số điện thoại</span>
                    <div class="info-cell-value" :class="{ 'editing': isEditing }">
                      <input 
                        v-if="isEditing"
                        v-model="editableUser.phone"
                        type="tel"
                        placeholder="Nhập số điện thoại"
                        class="edit-input"
                      />
                      <span v-else class="read-only">{{ user.phone || 'Chưa cập nhật' }}</span>
                    </div>
                  </div>
                  <div class="info-cell">
                    <span class="info-cell-label">Ngày sinh</span>
                    <span class="info-cell-value read-only">{{ formatDate(user.dob) || 'Chưa cập nhật' }}</span>
                  </div>
                </div>
                <div class="info-row">
                  <div class="info-cell">
                    <span class="info-cell-label">Giới tính</span>
                    <span class="info-cell-value read-only">{{ getGenderText(user.gender) }}</span>
                  </div>
                  <div class="info-cell">
                    <span class="info-cell-label">Địa chỉ</span>
                    <div class="info-cell-value" :class="{ 'editing': isEditing }">
                      <input 
                        v-if="isEditing"
                        v-model="editableUser.address"
                        type="text"
                        placeholder="Nhập địa chỉ"
                        class="edit-input"
                      />
                      <span v-else class="read-only">{{ user.address || 'Chưa cập nhật' }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </BaseCard>

            <BaseCard class="info-card mt-6">
              <h3 class="section-title">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
                </svg>
                Thông tin công việc
              </h3>
              <div class="work-info">
                <div class="work-item">
                  <span class="work-label">Phòng ban</span>
                  <span class="work-value">{{ user.department || 'Chưa phân loại' }}</span>
                </div>
                <div class="work-item">
                  <span class="work-label">Chức vụ</span>
                  <span class="work-value">{{ user.position || 'Nhân viên' }}</span>
                </div>
                <div class="work-item">
                  <span class="work-label">Mã nhân viên</span>
                  <span class="work-value">{{ user.employeeId || 'NV-001' }}</span>
                </div>
                <div class="work-item">
                  <span class="work-label">Ngày bắt đầu</span>
                  <span class="work-value">{{ formatDate(user.startDate) || '01/11/2020' }}</span>
                </div>
                <div class="work-item">
                  <span class="work-label">Quản lý</span>
                  <span class="work-value">{{ user.manager || 'Chưa có' }}</span>
                </div>
                <div class="work-item">
                  <span class="work-label">Loại hợp đồng</span>
                  <span class="work-value">{{ user.employmentType || 'Chính thức' }}</span>
                </div>
              </div>
            </BaseCard>

            <BaseCard class="info-card mt-6">
              <h3 class="section-title">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
                </svg>
                Giới thiệu bản thân
              </h3>
              <div class="bio-container">
                <div v-if="!isEditing" class="bio-content">
                  <p>{{ user.bio || 'Chưa có giới thiệu' }}</p>
                </div>
                <textarea 
                  v-else
                  v-model="editableUser.bio" 
                  class="bio-textarea" 
                  rows="4" 
                  placeholder="Chia sẻ một chút về bản thân..."
                ></textarea>
                <div v-if="isEditing" class="bio-hint">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="16" x2="12" y2="12" />
                    <line x1="12" y1="8" x2="12.01" y2="8" />
                  </svg>
                  <span>Chia sẻ về kỹ năng, kinh nghiệm và mục tiêu của bạn</span>
                </div>
              </div>
            </BaseCard>
          </div>
        </div>
      </div>
    </main>

    <!-- Password Change Modal -->
    <div v-if="showPasswordModal" class="password-modal-overlay" @click.self="closePasswordModal">
      <div class="password-modal">
        <div class="password-modal-header">
          <h3>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0 1 10 0v4" />
            </svg>
            Đổi mật khẩu
          </h3>
          <button class="close-btn" @click="closePasswordModal">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <div class="password-modal-body">
          <div class="password-modal-body-left">
            <div class="password-field">
              <label>Mật khẩu hiện tại</label>
              <div class="password-input-wrapper">
                <input 
                  :type="showCurrentPassword ? 'text' : 'password'"
                  v-model="passwordForm.currentPassword"
                  placeholder="Nhập mật khẩu hiện tại"
                />
                <button type="button" class="toggle-password" @click="showCurrentPassword = !showCurrentPassword">
                  <span>{{ showCurrentPassword ? 'Ẩn' : 'Hiện' }}</span>
                </button>
              </div>
            </div>

            <div class="password-field">
              <label>Mật khẩu mới</label>
              <div class="password-input-wrapper">
                <input 
                  :type="showNewPassword ? 'text' : 'password'"
                  v-model="passwordForm.newPassword"
                  placeholder="Nhập mật khẩu mới"
                  @input="checkPasswordStrength"
                />
                <button type="button" class="toggle-password" @click="showNewPassword = !showNewPassword">
                  <span>{{ showNewPassword ? 'Ẩn' : 'Hiện' }}</span>
                </button>
              </div>
              
              <!-- Password Strength Indicator -->
              <div class="password-strength">
                <div class="strength-bar">
                  <div class="strength-segment" :class="{ 'active-strength-1': passwordStrength >= 1 }"></div>
                  <div class="strength-segment" :class="{ 'active-strength-2': passwordStrength >= 2 }"></div>
                  <div class="strength-segment" :class="{ 'active-strength-3': passwordStrength >= 3 }"></div>
                </div>
                <div class="strength-text">
                  Độ mạnh: <span>{{ strengthText }}</span>
                </div>
              </div>
            </div>

            <div class="password-field">
              <label>Xác nhận mật khẩu mới</label>
              <div class="password-input-wrapper">
                <input 
                  :type="showConfirmPassword ? 'text' : 'password'"
                  v-model="passwordForm.confirmPassword"
                  placeholder="Nhập lại mật khẩu mới"
                />
                <button type="button" class="toggle-password" @click="showConfirmPassword = !showConfirmPassword">
                  <span>{{ showConfirmPassword ? 'Ẩn' : 'Hiện' }}</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Password Requirements -->
          <div class="password-requirements">
            <div class="requirement-item" :class="{ met: hasMinLength }">
              <svg v-if="hasMinLength" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Ít nhất 8 ký tự</span>
            </div>
            <div class="requirement-item" :class="{ met: hasUpperCase }">
              <svg v-if="hasUpperCase" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Ít nhất 1 chữ hoa</span>
            </div>
            <div class="requirement-item" :class="{ met: hasLowerCase }">
              <svg v-if="hasLowerCase" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Ít nhất 1 chữ thường</span>
            </div>
            <div class="requirement-item" :class="{ met: hasNumber }">
              <svg v-if="hasNumber" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Ít nhất 1 số</span>
            </div>
            <div class="requirement-item" :class="{ met: hasSpecialChar }">
              <svg v-if="hasSpecialChar" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Ít nhất 1 ký tự đặc biệt</span>
            </div>
            <div class="requirement-item" :class="{ met: passwordsMatch && passwordForm.newPassword }">
              <svg v-if="passwordsMatch && passwordForm.newPassword" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#10b981" stroke-width="2">
                <path d="M20 6L9 17l-5-5" />
              </svg>
              <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#64748b" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>Mật khẩu xác nhận khớp</span>
            </div>
          </div>
        </div>
        <div class="password-modal-footer">
          <BaseButton variant="secondary" @click="closePasswordModal">Hủy</BaseButton>
          <BaseButton 
            variant="primary" 
            @click="changePassword"
            :disabled="!canChangePassword || passwordLoading"
          >
            {{ passwordLoading ? 'Đang xử lý...' : 'Cập nhật mật khẩu' }}
          </BaseButton>
        </div>
      </div>
    </div>

    <!-- QR Scanner Modal -->
    <QRScanner 
      v-if="showQRScanner" 
      @close="showQRScanner = false" 
      @scan-result="handleScanResult"
    />

    <!-- Personal QR Modal -->
    <div v-if="showPersonalQRModal" class="qr-modal-overlay" @click.self="closePersonalQR">
      <div class="qr-modal-content">
        <div class="qr-modal-header">
          <h3 class="qr-modal-title">MÃ QR CÁ NHÂN</h3>
          <button class="qr-close-btn" @click="closePersonalQR">×</button>
        </div>
        
        <div class="qr-modal-body">
          <div class="qr-two-columns">
            <!-- CỘT TRÁI: THÔNG TIN -->
            <div class="qr-info-column">
              <div class="qr-info-header">
                <div class="qr-info-avatar">{{ user.fullName?.charAt(0) }}{{ user.fullName?.split(' ').pop()?.charAt(0) }}</div>
                <div class="qr-info-title">
                  <h4>{{ user.fullName }}</h4>
                  <p>{{ user.employeeId }}</p>
                </div>
              </div>
              
              <div class="qr-info-list">
                <div class="qr-info-item">
                  <span class="qr-info-label">Mã nhân viên</span>
                  <span class="qr-info-value">{{ user.employeeId }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Họ tên</span>
                  <span class="qr-info-value">{{ user.fullName }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Phòng ban</span>
                  <span class="qr-info-value">{{ user.department }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Chức vụ</span>
                  <span class="qr-info-value">{{ user.position }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Email</span>
                  <span class="qr-info-value">{{ user.email }}</span>
                </div>
                <div class="qr-info-item">
                  <span class="qr-info-label">Số điện thoại</span>
                  <span class="qr-info-value">{{ user.phone || 'Chưa cập nhật' }}</span>
                </div>
              </div>
            </div>
            
            <!-- CỘT PHẢI: QR CODE -->
            <div class="qr-code-column">
              <div class="qr-code-container">
                <canvas ref="personalQRCanvas"></canvas>
              </div>
              <p class="qr-code-note">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                  <line x1="9" y1="9" x2="15" y2="15"></line>
                  <line x1="15" y1="9" x2="9" y2="15"></line>
                </svg>
                Quét mã để xem thông tin liên hệ
              </p>
            </div>
          </div>
        </div>
        
        <div class="qr-modal-footer">
          <BaseButton variant="primary" @click="downloadQR">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="7 10 12 15 17 10" />
              <line x1="12" y1="15" x2="12" y2="3" />
            </svg>
            Tải xuống QR
          </BaseButton>
          <BaseButton variant="secondary" @click="closePersonalQR">Đóng</BaseButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import api from '@/services/api'
import { useAuthStore } from '../stores/authStore'
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import BaseButton from './base/BaseButton.vue'
import '@/CSS/Setting.css'

const authStore = useAuthStore()
const isEditing = ref(false)
const showQRScanner = ref(false)
const fileInput = ref(null)
const loading = ref(false)
const error = ref(null)
const selectedFile = ref(null)
const showSuccessMessage = ref(false)
const successMessage = ref('')

// Personal QR Modal
const showPersonalQRModal = ref(false)
const personalQRCanvas = ref(null)

// Password Modal
const showPasswordModal = ref(false)
const passwordLoading = ref(false)
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const passwordStrength = ref(0)

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// Default avatar
const defaultAvatar = 'data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'100\' height=\'100\' viewBox=\'0 0 100 100\'%3E%3Crect width=\'100\' height=\'100\' fill=\'%236366f1\'/%3E%3Ctext x=\'50\' y=\'65\' font-size=\'40\' text-anchor=\'middle\' fill=\'white\' font-family=\'Arial\'%3ENV%3C/text%3E%3C/svg%3E'

// User data
const user = reactive({
  id: null,
  avatar: '',
  fullName: '',
  email: '',
  phone: '',
  dob: '',
  gender: '',
  address: '',
  employeeId: '',
  department: '',
  position: '',
  startDate: '',
  manager: '',
  employmentType: '',
  bio: ''
})

// Editable fields
const editableUser = reactive({
  phone: '',
  address: '',
  bio: ''
})

// Original data for cancel
const originalData = reactive({})

// Password validation computed
const hasMinLength = computed(() => passwordForm.newPassword.length >= 8)
const hasUpperCase = computed(() => /[A-Z]/.test(passwordForm.newPassword))
const hasLowerCase = computed(() => /[a-z]/.test(passwordForm.newPassword))
const hasNumber = computed(() => /[0-9]/.test(passwordForm.newPassword))
const hasSpecialChar = computed(() => /[!@#$%^&*(),.?":{}|<>]/.test(passwordForm.newPassword))
const passwordsMatch = computed(() => passwordForm.newPassword === passwordForm.confirmPassword)

const canChangePassword = computed(() => {
  return hasMinLength.value && 
         hasUpperCase.value && 
         hasLowerCase.value && 
         hasNumber.value && 
         hasSpecialChar.value && 
         passwordsMatch.value &&
         passwordForm.currentPassword
})

const strengthText = computed(() => {
  const score = passwordStrength.value
  if (score === 0) return 'Rất yếu'
  if (score === 1) return 'Yếu'
  if (score === 2) return 'Trung bình'
  if (score === 3) return 'Mạnh'
  return ''
})

const formatDate = (date) => {
  if (!date) return ''
  try {
    return new Date(date).toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).replace(/\//g, '/')
  } catch {
    return ''
  }
}

const getGenderText = (gender) => {
  const genders = {
    male: 'Nam',
    female: 'Nữ',
    other: 'Khác'
  }
  return genders[gender] || gender || 'Chưa cập nhật'
}

const enableEditing = () => {
  // Lưu dữ liệu gốc
  originalData.phone = user.phone
  originalData.address = user.address
  originalData.bio = user.bio
  
  // Copy vào editable
  editableUser.phone = user.phone || ''
  editableUser.address = user.address || ''
  editableUser.bio = user.bio || ''
  
  isEditing.value = true
}

const cancelEditing = () => {
  // Khôi phục dữ liệu gốc
  user.phone = originalData.phone
  user.address = originalData.address
  user.bio = originalData.bio
  
  // Xóa file đã chọn nếu có
  selectedFile.value = null
  if (fileInput.value) {
    fileInput.value.value = ''
  }
  
  isEditing.value = false
}

const saveChanges = async () => {
  loading.value = true
  error.value = null
  
  try {
    // Chỉ gửi các trường được phép chỉnh sửa
    const payload = {
      phone: editableUser.phone || '',
      address: editableUser.address || '',
      bio: editableUser.bio || ''
    }

    console.log('Updating profile with payload:', payload)
    
    // Update profile via auth API
    const response = await api.auth.updateProfile(payload)
    console.log('Update profile response:', response)

    // Nếu có ảnh mới, upload avatar
    if (selectedFile.value && user.id) {
      const fd = new FormData()
      fd.append('file', selectedFile.value)
      await api.employees.uploadAvatar(user.id, fd)
    }

    // Cập nhật user object
    user.phone = editableUser.phone
    user.address = editableUser.address
    user.bio = editableUser.bio

    // Cập nhật store
    if (authStore.user) {
      authStore.user = { 
        ...authStore.user, 
        phone: user.phone,
        address: user.address,
        bio: user.bio
      }
      localStorage.setItem('user', JSON.stringify(authStore.user))
    }

    // Hiển thị thông báo thành công
    showSuccess('Cập nhật thông tin thành công!')
    
    isEditing.value = false
  } catch (e) {
    error.value = e.message || String(e)
    console.error('Failed saving profile:', e)
    showSuccess('Có lỗi xảy ra khi cập nhật thông tin', 'error')
  } finally {
    loading.value = false
  }
}

const showSuccess = (message, type = 'success') => {
  successMessage.value = message
  showSuccessMessage.value = true
  setTimeout(() => {
    showSuccessMessage.value = false
  }, 3000)
}

const triggerFileInput = () => {
  if (isEditing.value) fileInput.value.click()
}

const handleAvatarChange = (event) => {
  const file = event.target.files[0]
  if (file) {
    // Kiểm tra kích thước file (max 2MB)
    if (file.size > 2 * 1024 * 1024) {
      alert('Ảnh không được vượt quá 2MB')
      return
    }
    
    // Kiểm tra định dạng
    if (!file.type.startsWith('image/')) {
      alert('Vui lòng chọn file ảnh')
      return
    }
    
    selectedFile.value = file
    const reader = new FileReader()
    reader.onload = (e) => user.avatar = e.target.result
    reader.readAsDataURL(file)
  }
}

const handleScanResult = (result) => {
  alert(`Chấm công thành công! Mã: ${result}`)
  showQRScanner.value = false
}

// Personal QR functions
async function showPersonalQR() {
  showPersonalQRModal.value = true
  
  await nextTick()
  await generatePersonalQR()
}

async function generatePersonalQR() {
  if (personalQRCanvas.value) {
    // Dữ liệu QR đầy đủ thông tin
    const data = `MÃ NV: ${user.employeeId}
HỌ TÊN: ${user.fullName}
PHÒNG BAN: ${user.department}
CHỨC VỤ: ${user.position}
`
    
    QRCode.toCanvas(personalQRCanvas.value, data, { 
      width: 220,
      margin: 2,
      color: {
        dark: '#1e293b',
        light: '#ffffff'
      }
    })
  }
}

function closePersonalQR() {
  showPersonalQRModal.value = false
}

function downloadQR() {
  if (personalQRCanvas.value) {
    const link = document.createElement('a')
    link.download = `QR-${user.employeeId || 'nhanvien'}.png`
    link.href = personalQRCanvas.value.toDataURL('image/png')
    link.click()
  }
}

// Password functions
const openPasswordModal = () => {
  showPasswordModal.value = true
  // Reset form
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
  passwordStrength.value = 0
}

const closePasswordModal = () => {
  showPasswordModal.value = false
}

const checkPasswordStrength = () => {
  const password = passwordForm.newPassword
  let score = 0
  
  if (password.length >= 8) score++
  if (/[A-Z]/.test(password)) score++
  if (/[a-z]/.test(password)) score++
  if (/[0-9]/.test(password)) score++
  if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score++
  
  passwordStrength.value = Math.min(3, Math.floor(score / 2))
}

const changePassword = async () => {
  if (!canChangePassword.value) return
  
  passwordLoading.value = true
  
  try {
    const response = await api.auth.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword
    })
    
    console.log('Change password response:', response)
    
    closePasswordModal()
    showSuccess('Đổi mật khẩu thành công!')
  } catch (e) {
    console.error('Failed to change password:', e)
    alert(e.message || 'Có lỗi xảy ra khi đổi mật khẩu')
  } finally {
    passwordLoading.value = false
  }
}

// Helper function to fill user data
function fillUserFromData(data) {
  if (!data) return
  
  user.id = data.id || data.employeeId || data.userId || null
  user.avatar = data.avatarUrl || data.avatar || ''
  user.fullName = data.fullName || data.name || data.username || ''
  user.email = data.email || ''
  user.phone = data.phone || ''
  user.dob = data.dob || data.birthDate || data.birthday || ''
  user.gender = data.gender || data.sex || ''
  user.address = data.address || ''
  user.employeeId = data.employeeId || data.employeeCode || 'NV-001'
  user.department = data.departmentName || data.department || 'Phòng Kỹ Thuật'
  user.position = data.position || data.jobTitle || 'Nhân viên'
  user.startDate = data.startDate || data.hireDate || '2020-11-01'
  user.manager = data.manager || data.managerName || ''
  user.employmentType = data.employmentType || data.contractType || 'Chính thức'
  user.bio = data.bio || ''
}

onMounted(async () => {
  loading.value = true

  // Nếu đã có user trên store, ưu tiên dùng
  if (authStore.user) {
    fillUserFromData(authStore.user)
  }

  try {
    const resp = await api.auth.profile()
    const data = resp?.data || resp
    if (data) {
      fillUserFromData(data)
      // Cập nhật store
      authStore.user = { ...authStore.user, ...data }
      localStorage.setItem('user', JSON.stringify(authStore.user))
    }
  } catch (e) {
    console.warn('Could not load profile from API, using local user data:', e)
  } finally {
    loading.value = false
  }
})
</script>