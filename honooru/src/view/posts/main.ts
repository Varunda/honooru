import Vue from "vue";

import Posts from "./Posts.vue";

const vm = new Vue({
	el: "#app",

	created: function(): void {

	},

	data: {

	},

	methods: {

	},

	components: {
		Posts
	}
});
(window as any).vm = vm;
