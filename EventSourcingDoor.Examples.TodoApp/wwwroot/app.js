const Goals = {
    template: '#goals-template',
    data() {
        return {goals: [], newGoalDescription: ""};
    },
    created() {
        fetch('api/goals')
            .then(response => response.json())
            .then(data => this.goals = data);
    },
    methods: {
        setGoal() {
            fetch(`api/goals?description=${encodeURIComponent(this.newGoalDescription)}`, {method: 'POST'})
                .then(response => response.json())
                .then(goalId => {
                    this.goals.push({
                        id: goalId,
                        description: this.newGoalDescription
                    });
                    this.newGoalDescription = "";
                });
        }
    }
};
const Tasks = {
    template: '#tasks-template',
    data() {
        return {tasks: [], newTaskDescription: ""};
    },
    created() {
        let goalId = this.$route.params.goalId;
        fetch(`api/goals/${encodeURIComponent(goalId)}/tasks`)
            .then(response => response.json())
            .then(data => this.tasks = data);
    },
    methods: {
        addTask() {
            let goalId = this.$route.params.goalId;
            fetch(`api/goals/${encodeURIComponent(goalId)}/tasks?description=${encodeURIComponent(this.newTaskDescription)}`, {method: 'POST'})
                .then(response => response.json())
                .then(taskId => {
                    this.tasks.push({
                        goalId: goalId,
                        id: taskId,
                        description: this.newTaskDescription,
                        isFinished: false
                    });
                    this.newTaskDescription = "";
                });
        },
        changeTask(task) {
            fetch(`api/goals/${encodeURIComponent(task.goalId)}/tasks/${encodeURIComponent(task.id)}?description=${encodeURIComponent(task.description)}`, {method: 'PUT'})
                .then(response => {
                    // TODO: Check status
                });
        },
        finishTask(task) {
            fetch(`api/goals/${encodeURIComponent(task.goalId)}/tasks/${encodeURIComponent(task.id)}`, {method: 'DELETE'})
                .then(response => {
                    // TODO: Check status
                });
        }
    }
};
Vue.directive('focus', {
    inserted: function (el) {
        el.focus()
    }
});
const app = new Vue({
    el: '#app',
    router: new VueRouter({
        routes: [
            {path: '/', component: Goals},
            {path: '/goal/:goalId', component: Tasks, name: 'tasks'}
        ]
    })
});