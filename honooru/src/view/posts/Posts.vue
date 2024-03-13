<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Posts
            </li>
        </app-menu>

        <hr class="border" />

        <div>

            <div style="display: grid; grid-template-columns: 300px 1fr; gap: 1rem;">
                <div>
                    <post-search v-model="query" @do-search="performSearch"></post-search>
                </div>

                <div>
                    <post-list :q="search"></post-list>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    export const Posts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                query: "" as string,
                search: "" as string,
            }
        },

        beforeMount: function(): void {
            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "sort:id_desc";
            }
            this.search = this.query;
        },

        methods: {
            performSearch: function(query: string): void {
                const url = new URL(location.href);
                url.searchParams.set("q", query.trim());
                history.pushState({ path: url.href }, "", `/posts?${url.searchParams.toString()}`);

                this.search = query;
                this.query = this.search;
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            PostList,
            PostSearch
        }
    });
    export default Posts;

</script>