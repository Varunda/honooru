<template>
    <span>
        <span v-if="account.state == 'idle'"></span>

        <span v-else-if="account.state == 'loading'">--</span>

        <span v-else-if="account.state == 'loaded'">
            {{account.data.name}}
        </span>
    </span>

</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppAccount, AppAccountApi } from "api/AppAccountApi";

    export const UserAccount = Vue.extend({
        props: {
            AccountId: { type: Number, required: true }
        },

        data: function() {
            return {
                account: Loadable.idle() as Loading<AppAccount>
            }
        },

        created: function(): void {
            this.bindAccount();
        },

        methods: {
            bindAccount: async function(): Promise<void> {
                this.account = Loadable.loading();
                this.account = await AppAccountApi.getByID(this.AccountId);
            }
        }

    });
    export default UserAccount;
</script>