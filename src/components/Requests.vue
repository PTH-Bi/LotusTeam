<template>
  <div class="requests-page">
    <Sidebar activeItem="Yêu cầu" />
    <main class="requests-main">
      <header class="requests-header glass">
        <h1 class="page-title">Phiếu yêu cầu</h1>
      </header>
      <div class="requests-content">
        <BaseCard class="placeholder-card flex-center">
          <div class="placeholder-content">
            <div class="placeholder-icon">📝</div>
            <h3>Tính năng đang phát triển</h3>
            <p>Hệ thống xử lý yêu cầu/đề xuất đang được thiết kế lại giao diện.</p>
          </div>
        </BaseCard>
      </div>
    </main>
  </div>
</template>

<script setup>
import Sidebar from './Sidebar.vue'
import BaseCard from './base/BaseCard.vue'
import '@/CSS/Requests.css'
</script>

