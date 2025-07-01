const chatbotBtn = document.getElementById("chatbot-btn");
const chatbotBox = document.getElementById("chatbot-box");
const chatbotClose = document.getElementById("chatbot-close");
const chat = document.getElementById("chat");
const userInput = document.getElementById("userInput");
const sendBtn = document.getElementById("chatbot-send-btn");
const faqBtns = document.querySelectorAll("#chatbot-faq button");

chatbotBtn.onclick = () => chatbotBox.style.display = "flex";
chatbotClose.onclick = () => chatbotBox.style.display = "none";

faqBtns.forEach(btn => {
    btn.onclick = () => {
        userInput.value = btn.innerText;
        send();
    }
});

function add(role, text) {
    // Format AI text: convert line breaks and lists to HTML
    let formatted = text;
    if (role === "AI") {
        // Convert numbered lists (1. ... 2. ...) to <ol>
        formatted = formatted.replace(/(?:^|\n)(\d+)\.\s(.+?)(?=(?:\n\d+\.|$))/gs, function(_, n, item) {
            return `\n<li>${item.trim()}</li>`;
        });
        if (/\n<li>/.test(formatted)) {
            formatted = formatted.replace(/(?:<li>.*<\/li>\s*)+/gs, function(match) {
                return `<ol>${match}</ol>`;
            });
        }
        // Convert dash/asterisk lists to <ul>
        formatted = formatted.replace(/(?:^|\n)[\-\*]\s(.+?)(?=(?:\n[\-\*]\s|$))/gs, function(_, item) {
            return `\n<li>${item.trim()}</li>`;
        });
        if (/\n<li>/.test(formatted)) {
            formatted = formatted.replace(/(?:<li>.*<\/li>\s*)+/gs, function(match) {
                return `<ul>${match}</ul>`;
            });
        }
        // Convert newlines to <br> (except inside lists)
        formatted = formatted.replace(/\n(?!<li>|<\/li>|<ol>|<ul>|<\/ol>|<\/ul>)/g, '<br>');
    }
    chat.innerHTML += `<div class="chat-msg ${role === "AI" ? "ai" : "user"}"><b>${role}:</b> ${formatted}</div>`;
    chat.scrollTop = chat.scrollHeight;
// Optional: Add some basic styles for chat bubbles and lists
if (!document.getElementById('chatbot-style')) {
    const style = document.createElement('style');
    style.id = 'chatbot-style';
    style.innerHTML = `
    .chat-msg { margin: 8px 0; line-height: 1.6; }
    .chat-msg.ai { background: #f5faff; border-radius: 8px; padding: 8px 12px; display: inline-block; }
    .chat-msg.user { background: #e3f2fd; border-radius: 8px; padding: 8px 12px; display: inline-block; }
    .chat-msg ol, .chat-msg ul { margin: 8px 0 8px 24px; padding-left: 18px; }
    .chat-msg li { margin-bottom: 2px; }
    `;
    document.head.appendChild(style);
}
}

async function send() {
    const msg = userInput.value.trim();
    if (!msg) return;
    add("Bạn", msg);
    userInput.value = "";
    add("AI", "<i>Đang trả lời...</i>");
    chat.scrollTop = chat.scrollHeight;

    const apiKey = typeof window.GROQ_API_KEY !== 'undefined' ? window.GROQ_API_KEY : "";
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
                            content: "Bạn là trợ lý AI của BusTicket, chuyên hỗ trợ các vấn đề liên quan đến đặt vé xe buýt. Hãy trả lời ngắn gọn, rõ ràng, chuyên nghiệp bằng tiếng Việt. Chỉ giải đáp các thắc mắc liên quan đến dịch vụ của BusTicket (ví dụ: cách đặt vé, lịch trình, giá vé, hủy vé, các chương trình khuyến mãi, thông tin về nhà xe, v.v.). Nếu người dùng hỏi về các chủ đề không liên quan đến dịch vụ của BusTicket, hãy lịch sự từ chối trả lời và nhắc họ rằng bạn chỉ hỗ trợ các vấn đề về vé xe buýt. Trang web của tôi hiện tại không hỗ trợ trên thiết bị di động và mobile app. Trang web của tôi vẫn chưa hỗ trợ tiếng Anh, chỉ hỗ trợ tiếng Việt. Khi người dùng quên mật khẩu, hãy liên hệ với administrator qua số điện thoại trang web hoặc email để được cấp lại tài khoản và mật khẩu mới. Trang web vẫn đang trong quá trình phát triển nên có thể sẽ gặp một số lỗi nhỏ. Hãy thông báo cho tôi nếu bạn gặp bất kỳ vấn đề nào để tôi có thể cải thiện. Chủ nhà xe có thể đăng ký mở bán vé trên hệ thống của chúng tôi bằng việc nhấn vào 'Mở bán vé trên hệ thống', ngoài ra chúng tôi còn có tuyển dụng tài xế xe buýt, có thể ứng tuyển bằng cách truy cập link 'Tuyển dụng' ở cuối trang web. Hướng dẫn đặt vé online trên hệ thống: 1. Truy cập trang web BusTicket. 2. Chọn điểm đi, điểm đến, ngày đi (chọn ngày về nếu muốn vé khứ hồi) và số lượng vé sau đó nhấn tìm kiếm để được chuyển đến trang Lịch trình, hoặc có thể truy cập Lịch trình ở thanh điều hướng, ngoài ra bạn có thể đặt vé bắng cách chọn vào 'Chuyến đi phổ biến' hoặc 'Tuyến đường phổ biến' nếu ở đó có chuyến bạn cần. 3. Nhấn vào nút 'Đặt vé' (hoặc 'Xem chi tiết'), tiếp theo nhập số điện thoại, chọn số ghế. 5. Xem lại thông tin vé, nếu đúng thì nhấn 'Thanh toán' để quét mã QR thanh toán, nếu sai sót thì đặt lại vé hoặc liên hệ trực tiếp với nhân viên hệ thống của trang web chúng tôi. 6. Sau khi thanh toán thành công, bạn sẽ nhận được mã vé, hãy lưu lại mã vé để tra cứu vé và đổi vé tại quầy vé trước khi lên xe. Số điện thoại trang web: 0393 769 711. Email: qh20812@gmail.com. Cách thanh toán: quét mã QR thanh toán trên vé khi đã chọn tuyến đường, thời gian, số ghế,... (lưu ý cho khách hàng: nếu quá 1 tiếng (60 phút) kể từ khi đặt vé mà không thanh toán thì vé đó sẽ tự hủy theo chính sách của hệ thống, hệ thống sẽ không chịu trách nhiệm nếu khách hàng khiếu nại về vấn đề này"
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