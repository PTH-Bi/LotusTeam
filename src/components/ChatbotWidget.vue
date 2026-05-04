<template>
  <div class="chatbot">
    <!-- Toggle Button -->
    <div class="chat-toggle" @click="toggleChat">
      💬
    </div>

    <!-- Chat Box -->
    <div v-if="open" class="chat-box">
      <div class="chat-header">
        <span>HR Chatbot</span>
        <button @click="toggleChat">✖</button>
      </div>

      <div class="chat-body" ref="chatBody">
        <div
          v-for="(msg, index) in messages"
          :key="index"
          :class="['message', msg.type]"
        >
          {{ msg.text }}
        </div>
        <div v-if="loading" class="message bot">
          Đang xử lý...
        </div>
      </div>

      <div class="chat-input">
        <input
          v-model="input"
          @keyup.enter="sendMessage"
          placeholder="Nhập câu hỏi..."
        />
        <button @click="sendMessage">Gửi</button>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "ChatbotWidget",
  data() {
    return {
      open: false,
      input: "",
      messages: [
        { type: "bot", text: "Xin chào, tôi có thể giúp gì cho bạn?" }
      ],
      loading: false
    };
  },
  methods: {
    toggleChat() {
      this.open = !this.open;
    },

    async sendMessage() {
      if (!this.input.trim()) return;

      const userMessage = this.input;
      this.messages.push({ type: "user", text: userMessage });
      this.input = "";
      this.loading = true;
      this.scrollToBottom();

      try {
        const res = await fetch("https://localhost:7010/api/chatbot/ask", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${localStorage.getItem("token")}`
          },
          body: JSON.stringify({ message: userMessage })
        });

        if (!res.ok) {
          let errorMsg = `Lỗi server (${res.status})`;
          
          if (res.status === 500) {
            errorMsg = "Lỗi cấu hình server, vui lòng liên hệ quản trị viên.";
          } else if (res.status === 401) {
            errorMsg = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại.";
          } else if (res.status === 404) {
            errorMsg = "Không tìm thấy API chatbot.";
          }
          
          throw new Error(errorMsg);
        }

        const data = await res.text();
        
        this.messages.push({
          type: "bot",
          text: data || "Xin lỗi, tôi chưa hiểu câu hỏi của bạn."
        });
      } catch (err) {
        console.error("Chat error:", err);
        
        let errorMessage = "Lỗi kết nối server!";
        
        if (err.message.includes("cấu hình")) {
          errorMessage = err.message;
        } else if (err.message.includes("Failed to fetch")) {
          errorMessage = "Không thể kết nối đến server. Kiểm tra lại kết nối mạng.";
        } else if (err.message.includes("401")) {
          errorMessage = "Vui lòng đăng nhập để sử dụng chatbot.";
        }
        
        this.messages.push({
          type: "bot",
          text: errorMessage
        });
      } finally {
        this.loading = false;
        this.scrollToBottom();
      }
    },

    scrollToBottom() {
      this.$nextTick(() => {
        const container = this.$refs.chatBody;
        if (container) {
          container.scrollTop = container.scrollHeight;
        }
      });
    }
  }
};
</script>

<style scoped>
.chatbot {
  position: fixed;
  bottom: 20px;
  right: 20px;
  z-index: 999;
}

.chat-toggle {
  width: 60px;
  height: 60px;
  background: #4caf50;
  border-radius: 50%;
  color: white;
  font-size: 26px;
  display: flex;
  justify-content: center;
  align-items: center;
  cursor: pointer;
}

.chat-box {
  width: 320px;
  height: 420px;
  background: white;
  border-radius: 10px;
  box-shadow: 0 5px 20px rgba(0,0,0,0.2);
  display: flex;
  flex-direction: column;
  margin-bottom: 10px;
}

.chat-header {
  background: #4caf50;
  color: white;
  padding: 10px;
  display: flex;
  justify-content: space-between;
}

.chat-header button {
  background: none;
  border: none;
  color: white;
  cursor: pointer;
  font-size: 16px;
}

.chat-body {
  flex: 1;
  padding: 10px;
  overflow-y: auto;
}

.message {
  margin: 5px 0;
  padding: 8px 10px;
  border-radius: 10px;
  max-width: 80%;
  word-wrap: break-word;
}

.message.user {
  background: #4caf50;
  color: white;
  margin-left: auto;
}

.message.bot {
  background: #eee;
}

.chat-input {
  display: flex;
  border-top: 1px solid #ddd;
}

.chat-input input {
  flex: 1;
  border: none;
  padding: 10px;
  outline: none;
}

.chat-input button {
  border: none;
  background: #4caf50;
  color: white;
  padding: 0 15px;
  cursor: pointer;
}

.chat-input button:hover {
  background: #45a049;
}
</style>