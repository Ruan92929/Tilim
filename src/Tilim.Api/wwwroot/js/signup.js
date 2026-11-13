// Sign-up page: a plain HTML form that POSTs to /api/workers and shows the result.
(() => {
  "use strict";

  const MAX_AMOUNTS = 6;
  const MIN_AMOUNTS = 1;

  const form = document.getElementById("signup-form");
  const amountsList = document.getElementById("amounts-list");
  const addAmountBtn = document.getElementById("add-amount");
  const slugInput = document.getElementById("slug");
  const slugPreview = document.getElementById("slug-preview");
  const pixKeyTypeSelect = document.getElementById("pixKeyType");
  const pixKeyInput = document.getElementById("pixKey");
  const pixKeyHint = document.getElementById("pixKey-hint");
  const messageInput = document.getElementById("thankYouMessage");
  const messageCount = document.getElementById("message-count");
  const formAlert = document.getElementById("form-alert");
  const submitBtn = document.getElementById("submit-btn");
  const successView = document.getElementById("success-view");
  const successLink = document.getElementById("success-link");
  const successLinkAnchor = document.getElementById("success-link-anchor");

  // Placeholder/hint text per key type — the server enforces real validation regardless.
  const PIX_KEY_HINTS = {
    Email: { placeholder: "joao@example.com", hint: "Um e-mail normal, igual ao cadastrado no seu banco." },
    Telefone: { placeholder: "+5511999999999", hint: "Com +55 na frente e só números depois, sem espaço." },
    Cpf: { placeholder: "12345678901", hint: "Só os 11 números do CPF, sem ponto nem traço." },
    Cnpj: { placeholder: "12345678000199", hint: "Só os 14 números do CNPJ, sem ponto nem traço." },
    Aleatoria: {
      placeholder: "123e4567-e12b-12d1-a456-426655440000",
      hint: "A \"chave aleatória\" que seu banco gerou, no formato com hífens."
    }
  };

  function updatePixKeyHint() {
    const info = PIX_KEY_HINTS[pixKeyTypeSelect.value];
    if (!info) return;
    pixKeyInput.placeholder = info.placeholder;
    pixKeyHint.textContent = info.hint;
  }

  pixKeyTypeSelect.addEventListener("change", updatePixKeyHint);
  updatePixKeyHint();

  const SLUG_MIN_LENGTH = 3;

  function updateSlugPreview() {
    const value = slugInput.value.trim().toLowerCase();
    if (!value) {
      slugPreview.textContent = "Sua página vai ficar em: /w/<endereço>";
      slugPreview.classList.remove("hint-warning");
      return;
    }

    if (value.length < SLUG_MIN_LENGTH) {
      const missing = SLUG_MIN_LENGTH - value.length;
      slugPreview.textContent =
        `Faltam ${missing} caractere${missing > 1 ? "s" : ""} (mínimo ${SLUG_MIN_LENGTH}).`;
      slugPreview.classList.add("hint-warning");
      return;
    }

    slugPreview.textContent = `Sua página vai ficar em: /w/${value}`;
    slugPreview.classList.remove("hint-warning");
  }

  slugInput.addEventListener("input", updateSlugPreview);

  messageInput.addEventListener("input", () => {
    messageCount.textContent = String(messageInput.value.length);
  });

  function amountRowTemplate(value) {
    const row = document.createElement("div");
    row.className = "amount-row";

    const input = document.createElement("input");
    input.type = "number";
    input.min = "0.01";
    input.max = "100000";
    input.step = "0.01";
    input.required = true;
    input.placeholder = "Ex: 10,00";
    input.className = "amount-input";
    if (value !== undefined) input.value = value;

    const removeBtn = document.createElement("button");
    removeBtn.type = "button";
    removeBtn.className = "icon-btn";
    removeBtn.title = "Remover este valor";
    removeBtn.textContent = "×";
    removeBtn.addEventListener("click", () => {
      if (amountsList.children.length <= MIN_AMOUNTS) return;
      row.remove();
      syncAddButtonState();
    });

    row.appendChild(input);
    row.appendChild(removeBtn);
    return row;
  }

  function syncAddButtonState() {
    addAmountBtn.classList.toggle("hidden", amountsList.children.length >= MAX_AMOUNTS);
  }

  addAmountBtn.addEventListener("click", () => {
    if (amountsList.children.length >= MAX_AMOUNTS) return;
    amountsList.appendChild(amountRowTemplate());
    syncAddButtonState();
  });

  [5, 10, 20].forEach((v) => amountsList.appendChild(amountRowTemplate(v)));
  syncAddButtonState();

  function collectAmounts() {
    return Array.from(amountsList.querySelectorAll(".amount-input"))
      .map((input) => Number(input.value))
      .filter((n) => Number.isFinite(n) && n > 0);
  }

  function showError(message) {
    formAlert.textContent = message;
    formAlert.classList.remove("hidden");
  }

  function hideError() {
    formAlert.classList.add("hidden");
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideError();

    const slugValue = slugInput.value.trim().toLowerCase();
    if (slugValue.length < SLUG_MIN_LENGTH) {
      showError(`O endereço da página deve ter pelo menos ${SLUG_MIN_LENGTH} caracteres.`);
      slugInput.focus();
      return;
    }

    const amounts = collectAmounts();
    if (amounts.length < MIN_AMOUNTS) {
      showError("Informe pelo menos um valor sugerido de gorjeta.");
      return;
    }

    const payload = {
      slug: slugValue,
      displayName: document.getElementById("displayName").value.trim(),
      pixKey: pixKeyInput.value.trim(),
      pixKeyType: pixKeyTypeSelect.value,
      city: document.getElementById("city").value.trim(),
      suggestedAmounts: amounts,
      thankYouMessage: messageInput.value.trim() || null
    };

    submitBtn.disabled = true;
    submitBtn.textContent = "Criando...";

    try {
      const response = await fetch("/api/workers", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      if (response.status === 201) {
        const body = await response.json();
        form.classList.add("hidden");
        successView.classList.remove("hidden");
        successLink.textContent = body.publicPageUrl;
        successLinkAnchor.href = body.publicPageUrl;
        return;
      }

      if (response.status === 429) {
        showError("Muitas tentativas em pouco tempo. Espere um minuto e tente de novo.");
        return;
      }

      let message = "Não foi possível criar sua página. Tente de novo.";
      try {
        const body = await response.json();
        if (body && body.message) message = body.message;
      } catch {
        // sem corpo JSON — mantém a mensagem genérica acima.
      }
      showError(message);
    } catch {
      showError("Não foi possível falar com o servidor. Confira sua conexão e tente de novo.");
    } finally {
      submitBtn.disabled = false;
      submitBtn.textContent = "Criar minha página";
    }
  });
})();
