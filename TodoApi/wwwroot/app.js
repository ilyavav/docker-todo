const taskForm = document.getElementById("task-form");
const taskNameInput = document.getElementById("task-name");
const addButton = document.getElementById("add-button");
const reloadButton = document.getElementById("reload-button");
const taskList = document.getElementById("tasks");
const statusText = document.getElementById("status");

function showError(error) {
    statusText.textContent = error.message;
    statusText.classList.add("error");
}

async function request(url, method = "GET", data = null) {
    const options = { method: method };

    if (data !== null) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(data);
    }

    const response = await fetch(url, options);

    if (!response.ok) {
        let message = `Ошибка запроса: ${response.status}.`;

        if (response.status === 400) {
            const error = await response.json();
            message = error.error || message;
        }

        if (response.status === 404) {
            message = "Задача не найдена. Обнови список.";
        }

        throw new Error(message);
    }

    if (response.status === 204) {
        return;
    }

    return await response.json();
}

async function loadTasks() {
    const tasks = await request("/todoitems");

    taskList.replaceChildren();

    for (const task of tasks) {
        taskList.appendChild(createTaskElement(task));
    }

    statusText.classList.remove("error");
    statusText.textContent = tasks.length === 0
        ? "Задач пока нет."
        : `Всего задач: ${tasks.length}`;
}

function createTaskElement(task) {
    const item = document.createElement("li");
    item.className = "task";

    if (task.isComplete) {
        item.classList.add("complete");
    }

    const label = document.createElement("label");
    label.className = "task-label";

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = task.isComplete;

    const name = document.createElement("span");
    name.className = "task-name";
    name.textContent = task.name;

    label.append(checkbox, name);

    const actions = document.createElement("div");
    actions.className = "actions";

    const editButton = document.createElement("button");
    editButton.type = "button";
    editButton.textContent = "Изменить";

    const deleteButton = document.createElement("button");
    deleteButton.type = "button";
    deleteButton.textContent = "Удалить";
    deleteButton.className = "delete-button";

    checkbox.addEventListener("change", async () => {
        checkbox.disabled = true;

        try {
            await request(`/todoitems/${task.id}`, "PUT", {
                name: task.name,
                isComplete: checkbox.checked
            });

            await loadTasks();
        } catch (error) {
            checkbox.checked = task.isComplete;
            showError(error);
        } finally {
            checkbox.disabled = false;
        }
    });

    editButton.addEventListener("click", async () => {
        const newName = prompt("Новое название задачи:", task.name);

        if (newName === null) {
            return;
        }

        if (newName.trim() === "") {
            showError(new Error("Укажи название задачи."));
            return;
        }

        editButton.disabled = true;

        try {
            await request(`/todoitems/${task.id}`, "PUT", {
                name: newName.trim(),
                isComplete: task.isComplete
            });

            await loadTasks();
        } catch (error) {
            showError(error);
        } finally {
            editButton.disabled = false;
        }
    });

    deleteButton.addEventListener("click", async () => {
        deleteButton.disabled = true;

        try {
            await request(`/todoitems/${task.id}`, "DELETE");
            await loadTasks();
        } catch (error) {
            showError(error);
        } finally {
            deleteButton.disabled = false;
        }
    });

    actions.append(editButton, deleteButton);
    item.append(label, actions);

    return item;
}

taskForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const name = taskNameInput.value.trim();

    if (name === "") {
        showError(new Error("Укажи название задачи."));
        return;
    }

    addButton.disabled = true;

    try {
        await request("/todoitems", "POST", {
            name: name,
            isComplete: false
        });

        taskNameInput.value = "";
        await loadTasks();
        taskNameInput.focus();
    } catch (error) {
        showError(error);
    } finally {
        addButton.disabled = false;
    }
});

reloadButton.addEventListener("click", () => {
    loadTasks().catch(showError);
});

loadTasks().catch(showError);