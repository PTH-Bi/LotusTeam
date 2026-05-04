<template>
  <div class="auth-page flex-center">
    <BaseCard :glass="true" class="login-card">
      <template #header>
        <div class="login-header">
          <div class="brand-logo glass">L</div>
          <h2 class="login-title">Chào mừng trở lại</h2>
          <p class="login-subtitle">Đăng nhập vào hệ thống Lotus HR</p>
        </div>
      </template>

      <form @submit.prevent="handleLogin" class="login-form">
        <BaseInput
          v-model="username"
          label="Tên đăng nhập"
          placeholder="Nhập tên đăng nhập"
          required
          autofocus
          :error="errors.username"
        >
          <template #icon>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21V19C20 17.9391 19.5786 16.9217 18.8284 16.1716C18.0783 15.4214 17.0609 15 16 15H8C6.93913 15 5.92172 15.4214 5.17157 16.1716C4.42143 16.9217 4 17.9391 4 19V21" />
              <path d="M12 11C14.2091 11 16 9.20914 16 7C16 4.79086 14.2091 3 12 3C9.79086 3 8 4.79086 8 7C8 9.20914 9.79086 11 12 11Z" />
            </svg>
          </template>
        </BaseInput>

        <BaseInput
          v-model="password"
          label="Mật khẩu"
          :type="showPassword ? 'text' : 'password'"
          placeholder="Nhập mật khẩu"
          required
          :error="errors.password"
        >
          <template #icon>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
              <path d="M7 11V7a5 5 0 0110 0v4" />
            </svg>
          </template>
          <template #append>
            <button type="button" class="toggle-btn" @click="showPassword = !showPassword">
              <svg v-if="!showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <circle cx="12" cy="12" r="3" />
              </svg>
              <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                <line x1="1" y1="1" x2="23" y2="23" />
              </svg>
            </button>
          </template>
        </BaseInput>

        <div v-if="authStore.error" class="error-box">
          {{ authStore.error }}
        </div>

        <BaseButton 
          type="submit" 
          variant="primary" 
          :loading="authStore.loading" 
          class="submit-btn"
        >
          Đăng nhập
          <template #icon-right>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M5 12h14M12 5l7 7-7 7" />
            </svg>
          </template>
        </BaseButton>

        <!-- Thêm thông tin về phiên đăng nhập và trạng thái kết nối -->
        <div class="login-info">
          <p class="session-info">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
            Phiên đăng nhập kéo dài 60 phút, tự động gia hạn
          </p>
          <p class="connection-info" :class="{ 'connected': connectionStatus === 'connected', 'disconnected': connectionStatus === 'disconnected' }">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="2" />
              <path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z" />
            </svg>
            {{ connectionStatusText }}
          </p>
        </div>
      </form>
    </BaseCard>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import BaseCard from './base/BaseCard.vue';
import BaseInput from './base/BaseInput.vue';
import BaseButton from './base/BaseButton.vue';
import { testConnection } from '../services/axiosClient';
import '@/CSS/Login.css';

const username = ref('');
const password = ref('');
const showPassword = ref(false);
const connectionStatus = ref('checking'); // 'checking', 'connected', 'disconnected'
const connectionStatusText = ref('Đang kiểm tra kết nối...');
const errors = reactive({
  username: '',
  password: ''
});
const authStore = useAuthStore();
const router = useRouter();

// Kiểm tra kết nối khi component mount
onMounted(async () => {
  await checkConnection();
});

const checkConnection = async () => {
  connectionStatus.value = 'checking';
  connectionStatusText.value = 'Đang kiểm tra kết nối...';
  
  const result = await testConnection();
  
  if (result.success) {
    connectionStatus.value = 'connected';
    connectionStatusText.value = 'Đã kết nối đến server';
  } else {
    connectionStatus.value = 'disconnected';
    connectionStatusText.value = 'Chế độ offline - Đang sử dụng dữ liệu mẫu';
    console.warn('Connection test failed:', result.message);
  }
};

const validateForm = () => {
  let isValid = true;
  
  if (!username.value.trim()) {
    errors.username = 'Vui lòng nhập tên đăng nhập';
    isValid = false;
  } else {
    errors.username = '';
  }
  
  if (!password.value) {
    errors.password = 'Vui lòng nhập mật khẩu';
    isValid = false;
  } else {
    errors.password = '';
  }
  
  return isValid;
};

const handleLogin = async () => {
  if (!validateForm()) return;
  
  const success = await authStore.login(username.value, password.value);
  if (success) {
    const role = authStore.user?.role || 'employee';
    const redirectTo = role === 'admin' ? '/dashboard' : '/setting';
    router.push(redirectTo);
  } else {
    // Nếu login thất bại, kiểm tra lại kết nối
    await checkConnection();
  }
};
</script>