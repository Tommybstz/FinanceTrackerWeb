const API = 'https://financetrackerweb.onrender.com/api/transactions';

async function fetchTransactions() {
    const response = await fetch(API);
    const transactions = await response.json();

    const container = document.getElementById('transactions');

    if (transactions.length === 0) {
        container.innerHTML = '<p>No transactions found.</p>';
        return;
    }


    container.innerHTML = transactions.reverse().map(tx => {

        const optionsDate = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
        };
        const typeClass = tx.type === "income" ? "income" : "expense";
        return `
        <div class="transaction-row ${typeClass}">
            <span>${tx.id}</span>
            <span>${tx.type}</span>
            <span>${tx.category}</span>
            <span>${tx.amount.toLocaleString("it-IT", { style: "currency", currency: "EUR" })}</span>
            <span>${new Date(tx.date).toLocaleDateString("it-IT", optionsDate)}</span>
            <span>${tx.note}</span>
        </div>
        `;
    }).join('');
}

async function addTransaction() {
    const type = document.getElementById('type').value;
    const category = document.getElementById('category').value;
    const amount = parseFloat(document.getElementById('amount').value);
    const date = document.getElementById('date').value || null;
    const note = document.getElementById('note').value;

    const response = await fetch(API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type, category, amount, date, note })
    });

    if (response.ok) {
        fetchTransactions();
    } else {
        const error = await response.text();
        alert(error);
    }
}

async function deleteTransaction() {
    const id = document.getElementById('deleteId').value;

    const response = await fetch(`${API}/${id}`, {
        method: 'DELETE'
    });

    if (response.ok) {
        fetchTransactions();
    } else {
        const error = await response.text();
        alert(error);
    }

}

async function updateTransaction() {
    const id = document.getElementById('updateId').value;
    const type = document.getElementById('updateType').value;
    const category = document.getElementById('updateCategory').value;
    const amount = parseFloat(document.getElementById('updateAmount').value) || null;
    const date = document.getElementById('updateDate').value || null;
    const note = document.getElementById('updateNote').value;

    const response = await fetch(`${API}/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type, category, amount, date, note })
    });

    if (response.ok) {
        fetchTransactions();
    } else {
        const error = await response.text();
        alert(error);
    }
}

async function viewSummary() {
    const response = await fetch(`${API}/summary`);
    const summary = await response.json();
    const container = document.getElementById('summary');

    container.innerHTML = `
  <div>Income: ${summary.totalIncome.toLocaleString("it-IT", { style: "currency", currency: "EUR" })}</div>
  <div>Expenses: ${summary.totalExpense.toLocaleString("it-IT", { style: "currency", currency: "EUR" })}</div>
  <div><b>Balance: ${summary.balance.toLocaleString("it-IT", { style: "currency", currency: "EUR" })}</b></div>
`;
}

async function deleteLastTransaction() {
    const response = await fetch(`${API}/last`, {
        method: 'DELETE'
    });

    if (response.ok) {
        fetchTransactions();
    } else {
        const error = await response.text();
        alert(error);
    }
}


fetchTransactions();