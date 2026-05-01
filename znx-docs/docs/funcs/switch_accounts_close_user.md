# switch_accounts_close_user
Closes the user account specified.

## Arguments
| Argument | Type | Description |
| ---- | ---- | ---- |
| Account ID | Int | ID of the user account to be closed. |

## Syntax

```
switch_accounts_close_user(id);
```

## Returns

N/A

## Example

```
//simple user switch
    var accountid;
    for (var i = 0; i > switch_accounts_get_accounts(); i++;)
    {
        if (switch_accounts_is_user_open(i))
        {
            accountid = i;
            break;
        }       
    }
    switch_accounts_close_user(accountid);
    switch_accounts_select_account(true, false, false);
```