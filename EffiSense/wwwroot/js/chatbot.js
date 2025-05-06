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
    let historyLoadedOrLoading = false;

    function loadChatHistory() {
        if (historyLoadedOrLoading) return; // Don't load if already loading or loaded this session

        historyLoadedOrLoading = true; // Mark as loading
        $chatBody.empty(); // Clear previous messages (like default welcome)

        $.ajax({
            url: '/Usages/GetChatHistory', // New endpoint
            type: 'GET',
            dataType: 'json',
            success: function (messages) {
                if (messages && messages.length > 0) {
                    messages.forEach(function (msg) {
                        // Append history messages instantly, no word-by-word animation
                        appendMessage(msg.text, msg.senderType === 'user' ? 'user-message' : 'bot-message', true);
                    });
                } else {
                    // If no history, display the initial bot welcome message
                    appendMessage("Hello! I'm your EffiSense assistant. How can I help you today?", 'bot-message', true);
                }
                scrollToBottom(true); // Instant scroll
            },
            error: function (xhr, status, error) {
                console.error("Error loading chat history:", status, error, xhr.responseText);
                // Fallback to default welcome message on error
                appendMessage("Hello! I'm your EffiSense assistant. How can I help you today?", 'bot-message', true);
                scrollToBottom(true);
            }
            // Not setting historyLoadedOrLoading to false here, it's per "chat open" session
        });
    }

    $chatbotFab.on('click', toggleChatWindow);
    $closeBtn.on('click', toggleChatWindow);

    function toggleChatWindow() {
        isChatOpen = !isChatOpen;
        $chatWindow.toggleClass('open');
        $chatbotFab.toggleClass('fab-open');

        if (isChatOpen) {
            historyLoadedOrLoading = false; // Reset for new open session
            loadChatHistory();
        } else {
            // When closing, you might want to clear the chat body if you don't want messages
            // to persist visually until the next history load. For now, let's leave them.
            // $chatBody.empty(); // Optional: clear on close
        }
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

        // User message is appended instantly
        appendMessage(messageText, 'user-message', true);
        $chatInput.val('');
        clearTimeout(wordAppendTimeout);

        const showIndicatorDelay = 150; // Slightly adjusted
        setTimeout(() => {
            showTypingIndicator();

            $.ajax({
                url: '/Usages/GetDashboardSuggestion', // This now also saves messages
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(messageText),
                success: function (response) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        if (response.success) {
                            // Display bot's response with word-by-word animation
                            appendMessage(response.suggestion, 'bot-message', false);
                        } else {
                            appendMessage(response.message || "Sorry, I couldn't get a suggestion right now.", 'bot-message', false);
                        }
                    }, hideDurationEstimate);
                },
                error: function (xhr, status, error) {
                    hideTypingIndicator();
                    const hideDurationEstimate = 400;
                    setTimeout(() => {
                        console.error("AJAX Error:", status, error, xhr.responseText);
                        appendMessage("Sorry, there was an error connecting to the assistant. Please try again.", 'bot-message', false);
                    }, hideDurationEstimate);
                }
            });

        }, showIndicatorDelay);
    }

    // Modified appendMessage for instant vs. animated
    function appendMessage(text, messageClass, instant = false) {
        const $messageElement = $('<div></div>').addClass('message ' + messageClass);

        if (messageClass === 'bot-message' && !instant) {
            $chatBody.append($messageElement);
            // scrollToBottom(); // Scroll when container is ready

            const wordsAndSpaces = text.split(/(\s+)/);
            let wordIndex = 0;
            const wordFadeDelay = 70;

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
            scrollToBottom(instant); // Pass instant flag
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

    function scrollToBottom(instant = false) {
        const scrollTopVal = $chatBody.prop("scrollHeight");
        if (instant) {
            $chatBody.scrollTop(scrollTopVal);
        } else {
            $chatBody.animate({
                scrollTop: scrollTopVal
            }, 200);
        }
    }

    // (Keep the keyframes check/insert code)
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