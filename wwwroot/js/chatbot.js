const chatbotBtn = document.getElementById("chatbot-btn");
const chatbotBox = document.getElementById("chatbot-box");
const chatbotClose = document.getElementById("chatbot-close");
const chat = document.getElementById("chat");
const userInput = document.getElementById("userInput");
const sendBtn = document.getElementById("chatbot-send-btn");
const faqBtns = document.querySelectorAll("#chatbot-faq button");

// Load environment variables
require('dotenv').config();

chatbotBtn.onclick = () => chatbotBox.style.display = "flex";
chatbotClose.onclick = () => chatbotBox.style.display = "none";

faqBtns.forEach(btn => {
    btn.onclick = () => {
        userInput.value = btn.innerText;
        send();
    }
});

function add(role, text) {
    chat.innerHTML += `<p><b>${role}:</b> ${text}</p>`;
    chat.scrollTop = chat.scrollHeight;
}

async function send() {
    const msg = userInput.value.trim();
    if (!msg) return;
    add("Bạn", msg);
    userInput.value = "";
    add("AI", "<i>Đang trả lời...</i>");
    chat.scrollTop = chat.scrollHeight;

    const apiKey = process.env.GROQ_API_KEY;
    try {
        const res = await fetch(
            "https://api.groq.com/openai/v1/chat/completions", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + apiKey,
                },
                body: JSON.stringify({
                    model: "meta-llama/llama-4-scout-17b-16e-instruct",
                    messages: [
                        {
                            role: "system",
                            content: "Bạn là trợ lý AI của BusTicket, chuyên hỗ trợ các vấn đề liên quan đến đặt vé xe buýt. Hãy trả lời ngắn gọn, rõ ràng, chuyên nghiệp bằng tiếng Việt. Chỉ giải đáp các thắc mắc liên quan đến dịch vụ của BusTicket (ví dụ: cách đặt vé, lịch trình, giá vé, hủy vé, các chương trình khuyến mãi, thông tin về nhà xe, v.v.). Nếu người dùng hỏi về các chủ đề không liên quan đến dịch vụ của BusTicket, hãy lịch sự từ chối trả lời và nhắc họ rằng bạn chỉ hỗ trợ các vấn đề về vé xe buýt."
                        },
                        {
                            role: "user",
                            content: msg,
                        },
                    ],
                }),
            }
        );
        const data = await res.json();
        chat.lastChild.remove();
        add("AI", data.choices[0].message.content);
    } catch (e) {
        chat.lastChild.remove();
        add("AI", "Xin lỗi, hiện tại tôi không thể trả lời. Vui lòng thử lại sau.");
    }
}

userInput.addEventListener("keydown", function (event) {
    if (event.key === "Enter") send();
});
sendBtn.onclick = send;