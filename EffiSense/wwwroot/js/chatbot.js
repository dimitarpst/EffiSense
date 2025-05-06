$(function () {
    const $chatbotFab = $('#chatbot-fab');
    const $chatWindow = $('#chatbot-window');
    const $closeBtn = $('#chatbot-close-btn');
    const $sendBtn = $('#chatbot-send-btn');
    const $chatInput = $('#chatbot-input');
    const $chatBody = $('#chat-body');
    let isChatOpen = false;
    let typingIndicatorTimeout;
    let wordAppendTimeout;

    $chatbotFab.on('click', toggleChatWindow);
    $closeBtn.on('click', toggleChatWindow);

    function toggleChatWindow() {
        isChatOpen = !isChatOpen;
        $chatWindow.toggleClass('open');
        $chatbotFab.toggleClass('fab-open');
    }

    $sendBtn.on('click', sendMessage);
    $chatInput.on('keypress', function (e) {
        if (e.key === 'Enter' && $chatInput.val().trim() !== '') {
            e.preventDefault();
            sendMessage();
        }
    });

    function sendMessage() {
        const messageText = $chatInput.val().trim();
        if (messageText === '') {
            return;
        }

        displayUserMessage(messageText);
        $chatInput.val('');
        clearTimeout(wordAppendTimeout);

        const showIndicatorDelay = 200;
        setTimeout(() => {
            showTypingIndicator();

            $.ajax({
                url: '/Usages/GetDashboardSuggestion',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(messageText),
                success: function (response) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        if (response.success) {
                            displayBotMessage(response.suggestion);
                        } else {
                            displayBotMessage(response.message || "Sorry, I couldn't get a suggestion right now.");
                        }
                    }, hideDurationEstimate);
                },
                error: function (xhr, status, error) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        console.error("AJAX Error:", status, error, xhr.responseText);
                        displayBotMessage("Sorry, there was an error connecting to the assistant. Please try again.");
                    }, hideDurationEstimate);
                }
            });

        }, showIndicatorDelay);
    }

    function displayUserMessage(text) {
        appendMessage(text, 'user-message');
    }

    function displayBotMessage(text) {
        appendMessage(text, 'bot-message');
    }

    function appendMessage(text, messageClass) {
        const $messageElement = $('<div></div>').addClass('message ' + messageClass);

        if (messageClass === 'bot-message') {
            $chatBody.append($messageElement);
            scrollToBottom();

            const wordsAndSpaces = text.split(/(\s+)/); 
            let wordIndex = 0;
            const wordFadeDelay = 45; 

            function animateNextWord() {
                if (wordIndex < wordsAndSpaces.length) {
                    const part = wordsAndSpaces[wordIndex];
                    if (part.trim() !== '') { 
                        const $wordSpan = $('<span></span>')
                            .addClass('word-to-animate') 
                            .text(part);
                        $messageElement.append($wordSpan);

                        setTimeout(() => {
                            $wordSpan.css('opacity', 1);
                        }, 10);

                    } else { 
                        $messageElement.append(document.createTextNode(part));
                    }

                    scrollToBottom(); 
                    wordIndex++;
                    wordAppendTimeout = setTimeout(animateNextWord, wordFadeDelay);
                }
            }

            const initialDelayForBubble = 50; 
            wordAppendTimeout = setTimeout(animateNextWord, initialDelayForBubble);


        } else {
            $messageElement.text(text);
            $chatBody.append($messageElement);
            scrollToBottom();
        }
    }

    function showTypingIndicator() {
        clearTimeout(typingIndicatorTimeout);
        $('#typing-indicator-element').remove();
        clearTimeout(wordAppendTimeout);

        const $indicatorElement = $('<div></div>')
            .addClass('message bot-message typing-indicator')
            .attr('id', 'typing-indicator-element')
            .html(`
                <span></span>
                <span></span>
                <span></span>
            `);
        $chatBody.append($indicatorElement);
        scrollToBottom();
    }

    function hideTypingIndicator() {
        clearTimeout(typingIndicatorTimeout);
        typingIndicatorTimeout = setTimeout(() => {
            const $indicator = $('#typing-indicator-element');
            if ($indicator.length) {
                $indicator.css('animation', 'messageFadeOut 0.3s ease forwards');
                setTimeout(() => {
                    if ($('#typing-indicator-element').is($indicator)) {
                        $indicator.remove();
                    }
                }, 300);
            }
        }, 100);
    }

    function scrollToBottom() {
        $chatBody.animate({
            scrollTop: $chatBody.prop("scrollHeight")
        }, 200);
    }

    const styleSheet = document.styleSheets[0];
    let ruleExists = false;
    try {
        if (styleSheet) {
            for (let i = 0; i < styleSheet.cssRules.length; i++) {
                if (styleSheet.cssRules[i].type === CSSRule.KEYFRAMES_RULE && styleSheet.cssRules[i].name === 'messageFadeOut') {
                    ruleExists = true;
                    break;
                }
            }
            if (!ruleExists) {
                styleSheet.insertRule(`
                    @keyframes messageFadeOut {
                        from { opacity: 1; transform: scale(1) translateY(0); }
                        to { opacity: 0; transform: scale(0.9) translateY(10px); }
                    }
                `, styleSheet.cssRules.length);
            }
        }
    } catch (e) {
        // console.warn("Could not check/insert fadeOut keyframes: ", e);
    }
});