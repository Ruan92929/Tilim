// Public tipping page for one worker. Reads the slug from the URL and
// fills itself in from GET /api/w/{slug}.
//
// Security: displayName/thankYouMessage are untrusted worker input, always
// set via textContent (never innerHTML) to avoid stored XSS.
(() => {
  "use strict";

  const slug = decodeURIComponent(
    window.location.pathname.replace(/\/+$/, "").split("/").pop()
  );

  const card = document.getElementById("card");
  const modalOverlay = document.getElementById("qr-modal-overlay");
  const modalImg = document.getElementById("qr-modal-img");
  const modalClose = document.getElementById("qr-modal-close");
  const modalPrint = document.getElementById("qr-modal-print");

  function openQrModal(src) {
    modalImg.src = src;
    modalOverlay.classList.remove("hidden");
  }

  function closeQrModal() {
    modalOverlay.classList.add("hidden");
  }

  modalClose.addEventListener("click", closeQrModal);
  modalOverlay.addEventListener("click", (event) => {
    if (event.target === modalOverlay) closeQrModal();
  });
  modalPrint.addEventListener("click", () => window.print());
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeQrModal();
  });

  function renderNotFound() {
    card.innerHTML = "";
    const msg = document.createElement("div");
    msg.className = "state-message";
    msg.textContent = "Essa página não existe ou foi desativada.";
    card.appendChild(msg);
  }

  function renderError() {
    card.innerHTML = "";
    const msg = document.createElement("div");
    msg.className = "state-message";
    msg.textContent = "Não foi possível carregar essa página agora. Tente de novo em instantes.";
    card.appendChild(msg);
  }

  function formatBRL(amount) {
    return amount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
  }

  function qrUrl(amount) {
    const base = `/api/w/${encodeURIComponent(slug)}/qrcode.png`;
    return amount ? `${base}?amount=${encodeURIComponent(amount.toFixed(2))}` : base;
  }

  function render(profile) {
    card.innerHTML = "";

    const name = document.createElement("h1");
    name.className = "public-name";
    name.textContent = profile.displayName;
    card.appendChild(name);

    if (profile.thankYouMessage) {
      const message = document.createElement("p");
      message.className = "public-message";
      message.textContent = profile.thankYouMessage;
      card.appendChild(message);
    }

    const qrWrap = document.createElement("div");
    qrWrap.className = "qr-wrap";
    qrWrap.title = "Toque para ampliar";
    const qrImg = document.createElement("img");
    qrImg.alt = `QR Code Pix de ${profile.displayName}`;
    qrImg.src = qrUrl(null);
    qrWrap.appendChild(qrImg);

    const zoomBadge = document.createElement("span");
    zoomBadge.className = "qr-zoom-badge";
    zoomBadge.setAttribute("aria-hidden", "true");
    zoomBadge.textContent = "⤢";
    qrWrap.appendChild(zoomBadge);

    qrWrap.addEventListener("click", () => openQrModal(qrImg.src));
    card.appendChild(qrWrap);

    let selectedAmount = null;

    if (profile.suggestedAmounts && profile.suggestedAmounts.length > 0) {
      const chipsWrap = document.createElement("div");
      chipsWrap.className = "amount-chips";

      const chips = profile.suggestedAmounts.map((amount) => {
        const chip = document.createElement("button");
        chip.type = "button";
        chip.className = "amount-chip";
        chip.textContent = formatBRL(amount);
        chip.addEventListener("click", () => {
          const isSelected = chip.classList.contains("selected");
          chips.forEach((c) => c.classList.remove("selected"));
          if (isSelected) {
            selectedAmount = null;
          } else {
            chip.classList.add("selected");
            selectedAmount = amount;
          }
          qrImg.src = qrUrl(selectedAmount);
        });
        return chip;
      });

      chips.forEach((chip) => chipsWrap.appendChild(chip));
      card.appendChild(chipsWrap);
    }

    const copyBtn = document.createElement("button");
    copyBtn.type = "button";
    copyBtn.className = "primary-btn print-hide";
    copyBtn.textContent = "Copiar código Pix";
    copyBtn.addEventListener("click", async () => {
      const original = copyBtn.textContent;
      try {
        await copyToClipboard(profile.pixCopyPaste);
        copyBtn.textContent = "Copiado!";
      } catch {
        copyBtn.textContent = "Não foi possível copiar";
      } finally {
        setTimeout(() => {
          copyBtn.textContent = original;
        }, 2000);
      }
    });
    card.appendChild(copyBtn);

    const printBtn = document.createElement("button");
    printBtn.type = "button";
    printBtn.className = "secondary-btn print-hide";
    printBtn.textContent = "Imprimir pra deixar na mesa";
    printBtn.addEventListener("click", () => window.print());
    card.appendChild(printBtn);
  }

  async function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }
    // Fallback for non-secure contexts, where the Clipboard API is unavailable.
    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.style.position = "fixed";
    textarea.style.opacity = "0";
    document.body.appendChild(textarea);
    textarea.focus();
    textarea.select();
    try {
      document.execCommand("copy");
    } finally {
      textarea.remove();
    }
  }

  async function load() {
    if (!slug) {
      renderNotFound();
      return;
    }

    try {
      const response = await fetch(`/api/w/${encodeURIComponent(slug)}`);
      if (response.status === 404) {
        renderNotFound();
        return;
      }
      if (!response.ok) {
        renderError();
        return;
      }
      const profile = await response.json();
      render(profile);
    } catch {
      renderError();
    }
  }

  load();
})();
